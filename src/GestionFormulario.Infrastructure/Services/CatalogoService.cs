using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Catalogos;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Services;

public sealed class CatalogoService(
    AppDbContext db,
    IValidator<CatalogoCreateRequest> createValidator,
    IValidator<CatalogoUpdateRequest> updateValidator,
    IValidator<CatalogoItemCreateRequest> itemCreateValidator,
    IValidator<CatalogoItemUpdateRequest> itemUpdateValidator,
    IValidator<ImportarCatalogoRequest> importValidator) : ICatalogoService
{
    public async Task<IReadOnlyList<CatalogoResumenDto>> ListAsync(
        bool soloActivos = true,
        CancellationToken cancellationToken = default)
    {
        var query = db.Catalogos.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (soloActivos)
        {
            query = query.Where(x => x.Activo);
        }

        var catalogos = await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        return catalogos.Select(x => new CatalogoResumenDto(
            x.Id, x.Nombre, x.Descripcion, x.Activo, x.CatalogoPadreId, x.Items.Count)).ToList();
    }

    public async Task<CatalogoDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var catalogo = await db.Catalogos
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "CATALOGO_NO_ENCONTRADO", "El catálogo no existe.");
        return Map(catalogo);
    }

    public async Task<CatalogoDto> CreateAsync(
        CatalogoCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await ValidarNombreUnicoAsync(request.Nombre, null, cancellationToken);
        await ValidarCatalogoPadreAsync(null, request.CatalogoPadreId, cancellationToken);

        var catalogo = new Catalogo
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo,
            CatalogoPadreId = request.CatalogoPadreId
        };

        db.Catalogos.Add(catalogo);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(catalogo.Id, cancellationToken);
    }

    public async Task<CatalogoDto> UpdateAsync(
        Guid id,
        CatalogoUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var catalogo = await CatalogoConItems(id, cancellationToken)
            ?? throw new AppException(404, "CATALOGO_NO_ENCONTRADO", "El catálogo no existe.");

        await ValidarNombreUnicoAsync(request.Nombre, id, cancellationToken);
        await ValidarCatalogoPadreAsync(id, request.CatalogoPadreId, cancellationToken);

        catalogo.Nombre = request.Nombre.Trim();
        catalogo.Descripcion = request.Descripcion?.Trim();
        catalogo.Activo = request.Activo;
        catalogo.CatalogoPadreId = request.CatalogoPadreId;

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await db.Catalogos.AnyAsync(x => x.Id == id, cancellationToken))
        {
            throw new AppException(404, "CATALOGO_NO_ENCONTRADO", "El catálogo no existe.");
        }

        var ids = new HashSet<Guid> { id };
        bool agregados;
        do
        {
            agregados = false;
            var hijos = await db.Catalogos
                .Where(x => x.CatalogoPadreId.HasValue && ids.Contains(x.CatalogoPadreId.Value))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            foreach (var hijo in hijos)
            {
                agregados |= ids.Add(hijo);
            }
        } while (agregados);

        var catalogos = await db.Catalogos.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var catalogo in catalogos)
        {
            catalogo.Activo = false;
        }

        var items = await db.CatalogoItems.Where(x => ids.Contains(x.CatalogoId)).ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.Activo = false;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogoDto>> ObtenerOpcionesAsync(
        Guid catalogoId,
        Guid? padreItemId,
        string? buscar,
        bool soloActivos = true,
        CancellationToken cancellationToken = default)
    {
        var catalogo = await db.Catalogos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == catalogoId, cancellationToken)
            ?? throw new AppException(404, "CATALOGO_NO_ENCONTRADO", "El catálogo no existe.");

        if (soloActivos && !catalogo.Activo)
        {
            return Array.Empty<OpcionCatalogoDto>();
        }

        var query = db.CatalogoItems.AsNoTracking().Where(x => x.CatalogoId == catalogoId);

        if (catalogo.CatalogoPadreId.HasValue)
        {
            if (!padreItemId.HasValue)
            {
                return Array.Empty<OpcionCatalogoDto>();
            }

            var padreValido = await db.CatalogoItems.AnyAsync(
                x => x.Id == padreItemId.Value &&
                     x.CatalogoId == catalogo.CatalogoPadreId.Value &&
                     (!soloActivos || x.Activo),
                cancellationToken);
            if (!padreValido)
            {
                return Array.Empty<OpcionCatalogoDto>();
            }
        }

        if (padreItemId.HasValue)
        {
            query = query.Where(x => x.PadreItemId == padreItemId.Value);
        }

        if (soloActivos)
        {
            query = query.Where(x => x.Activo);
        }

        var filtro = buscar?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            query = query.Where(x => x.Texto.ToLower().Contains(filtro) || x.Codigo.ToLower().Contains(filtro));
        }

        var items = await query
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.Texto)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(x => new OpcionCatalogoDto(x.Id, x.Codigo, x.Texto)).ToList();
    }

    public async Task<CatalogoItemDto> CreateItemAsync(
        Guid catalogoId,
        CatalogoItemCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await itemCreateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var catalogo = await ObtenerCatalogoAsync(catalogoId, cancellationToken);
        await ValidarCodigoItemUnicoAsync(catalogoId, request.Codigo, null, cancellationToken);
        await ValidarItemPadreAsync(catalogo, request.PadreItemId, null, cancellationToken);

        var item = new CatalogoItem
        {
            Id = Guid.NewGuid(),
            CatalogoId = catalogoId,
            Codigo = request.Codigo.Trim(),
            Texto = request.Texto.Trim(),
            Orden = request.Orden,
            Activo = request.Activo,
            PadreItemId = request.PadreItemId
        };

        db.CatalogoItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task<CatalogoItemDto> UpdateItemAsync(
        Guid itemId,
        CatalogoItemUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await itemUpdateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var item = await db.CatalogoItems
            .Include(x => x.Catalogo)
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new AppException(404, "ITEM_NO_ENCONTRADO", "El ítem no existe.");

        await ValidarCodigoItemUnicoAsync(item.CatalogoId, request.Codigo, itemId, cancellationToken);
        await ValidarItemPadreAsync(item.Catalogo, request.PadreItemId, itemId, cancellationToken);

        item.Codigo = request.Codigo.Trim();
        item.Texto = request.Texto.Trim();
        item.Orden = request.Orden;
        item.Activo = request.Activo;
        item.PadreItemId = request.PadreItemId;

        await db.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await db.CatalogoItems.FindAsync([itemId], cancellationToken)
            ?? throw new AppException(404, "ITEM_NO_ENCONTRADO", "El ítem no existe.");

        var pendientes = new Queue<Guid>();
        pendientes.Enqueue(itemId);
        while (pendientes.Count > 0)
        {
            var actual = pendientes.Dequeue();
            var hijos = await db.CatalogoItems
                .Where(x => x.PadreItemId == actual && x.Activo)
                .ToListAsync(cancellationToken);
            foreach (var hijo in hijos)
            {
                hijo.Activo = false;
                pendientes.Enqueue(hijo.Id);
            }
        }

        item.Activo = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportacionResultadoDto> ImportarAsync(
        Guid catalogoId,
        ImportarCatalogoRequest request,
        CancellationToken cancellationToken = default)
    {
        await importValidator.ValidateAndThrowAsync(request, cancellationToken);

        var catalogo = await ObtenerCatalogoAsync(catalogoId, cancellationToken);
        var items = await db.CatalogoItems
            .Where(x => x.CatalogoId == catalogoId)
            .ToListAsync(cancellationToken);

        using var memoria = new MemoryStream();
        await request.Contenido.CopyToAsync(memoria, cancellationToken);
        memoria.Position = 0;

        var filas = request.NombreArchivo.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? LeerExcel(memoria)
            : LeerCsv(memoria);

        var errores = new List<string>();
        var codigosArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var procesadas = 0;
        var creadas = 0;
        var actualizadas = 0;

        foreach (var fila in filas)
        {
            procesadas++;
            if (!string.IsNullOrWhiteSpace(fila.Error))
            {
                errores.Add($"Fila {fila.Numero}: {fila.Error}");
                continue;
            }

            if (!codigosArchivo.Add(fila.Codigo))
            {
                errores.Add($"Fila {fila.Numero}: código duplicado '{fila.Codigo}'.");
                continue;
            }

            var errorPadre = await ResolverPadreItemAsync(catalogo, fila.PadreItemCodigo, cancellationToken);
            if (errorPadre is not null)
            {
                errores.Add($"Fila {fila.Numero}: {errorPadre}");
                continue;
            }

            var padreItemId = await BuscarPadreItemIdAsync(catalogo, fila.PadreItemCodigo, cancellationToken);
            var existente = items.FirstOrDefault(
                x => string.Equals(x.Codigo, fila.Codigo, StringComparison.OrdinalIgnoreCase));

            if (existente is null)
            {
                var item = new CatalogoItem
                {
                    Id = Guid.NewGuid(),
                    CatalogoId = catalogoId,
                    Codigo = fila.Codigo,
                    Texto = fila.Texto,
                    Orden = fila.Orden,
                    Activo = true,
                    PadreItemId = padreItemId
                };
                db.CatalogoItems.Add(item);
                items.Add(item);
                creadas++;
            }
            else
            {
                existente.Texto = fila.Texto;
                existente.Orden = fila.Orden;
                existente.Activo = true;
                existente.PadreItemId = padreItemId;
                actualizadas++;
            }
        }

        var desactivadas = 0;
        if (request.Reemplazar)
        {
            foreach (var item in items.Where(x => !codigosArchivo.Contains(x.Codigo) && x.Activo))
            {
                item.Activo = false;
                desactivadas++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ImportacionResultadoDto(procesadas, creadas, actualizadas, desactivadas, errores);
    }

    private static List<FilaImportacion> LeerExcel(Stream stream)
    {
        using var libro = new XLWorkbook(stream);
        var hoja = libro.Worksheet(1);
        var filas = hoja.RowsUsed().ToList();
        if (filas.Count == 0)
        {
            throw new AppException(400, "ARCHIVO_VACIO", "El archivo no contiene datos.");
        }

        var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var celda in filas[0].CellsUsed())
        {
            columnas[Normalizar(celda.GetString())] = celda.Address.ColumnNumber;
        }

        var resultado = new List<FilaImportacion>();
        for (var i = 1; i < filas.Count; i++)
        {
            var fila = filas[i];
            var codigo = Celda(fila, columnas, "codigo");
            var texto = Celda(fila, columnas, "texto", "descripcion", "nombre");
            var ordenTexto = Celda(fila, columnas, "orden");
            var padre = Celda(fila, columnas, "padreitemcodigo", "padreitemid", "padreitem");
            resultado.Add(ConstruirFila(i + 1, codigo, texto, ordenTexto, padre));
        }
        return resultado;
    }

    private static List<FilaImportacion> LeerCsv(Stream stream)
    {
        using var lector = CsvReader.Create(stream);
        lector.ReadHeader();
        var cabeceras = lector.Headers;
        if (cabeceras.Count == 0)
        {
            throw new AppException(400, "ARCHIVO_VACIO", "El archivo no contiene cabeceras.");
        }

        var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < cabeceras.Count; i++)
        {
            columnas[Normalizar(cabeceras[i])] = i;
        }

        var resultado = new List<FilaImportacion>();
        var numero = 1;
        while (lector.Read())
        {
            numero++;
            resultado.Add(ConstruirFila(
                numero,
                Campo(lector, columnas, "codigo"),
                Campo(lector, columnas, "texto", "descripcion", "nombre"),
                Campo(lector, columnas, "orden"),
                Campo(lector, columnas, "padreitemcodigo", "padreitemid", "padreitem")));
        }
        return resultado;
    }

    private static FilaImportacion ConstruirFila(
        int numero,
        string codigo,
        string texto,
        string ordenTexto,
        string padreItemCodigo)
    {
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(codigo)) errores.Add("falta Codigo");
        if (string.IsNullOrWhiteSpace(texto)) errores.Add("falta Texto");
        var orden = 0;
        if (!string.IsNullOrWhiteSpace(ordenTexto) && !int.TryParse(ordenTexto, out orden))
        {
            errores.Add("Orden no es un entero");
        }

        return new FilaImportacion(
            numero,
            codigo.Trim(),
            texto.Trim(),
            orden,
            padreItemCodigo.Trim(),
            errores.Count == 0 ? null : string.Join(", ", errores));
    }

    private static string Celda(IXLRow fila, IReadOnlyDictionary<string, int> columnas, params string[] nombres)
    {
        foreach (var nombre in nombres)
        {
            if (columnas.TryGetValue(nombre, out var indice))
            {
                return fila.Cell(indice).GetString().Trim();
            }
        }
        return string.Empty;
    }

    private static string Campo(CsvReader lector, IReadOnlyDictionary<string, int> columnas, params string[] nombres)
    {
        foreach (var nombre in nombres)
        {
            if (columnas.TryGetValue(nombre, out var indice) && indice < lector.FieldCount)
            {
                return lector.GetString(indice).Trim();
            }
        }
        return string.Empty;
    }

    private static string Normalizar(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder();
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
            {
                resultado.Append(char.ToLowerInvariant(c));
            }
        }
        return resultado.ToString();
    }

    private async Task<Catalogo> ObtenerCatalogoAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Catalogos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new AppException(404, "CATALOGO_NO_ENCONTRADO", "El catálogo no existe.");

    private async Task<Catalogo?> CatalogoConItems(Guid id, CancellationToken cancellationToken) =>
        await db.Catalogos
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private async Task ValidarNombreUnicoAsync(
        string nombre,
        Guid? ignorarId,
        CancellationToken cancellationToken)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        var existe = await db.Catalogos.AnyAsync(
            x => x.Nombre.ToLower() == normalizado && (!ignorarId.HasValue || x.Id != ignorarId.Value),
            cancellationToken);
        if (existe)
        {
            throw new AppException(409, "CATALOGO_DUPLICADO", "Ya existe un catálogo con ese nombre.");
        }
    }

    private async Task ValidarCatalogoPadreAsync(
        Guid? idActual,
        Guid? padreId,
        CancellationToken cancellationToken)
    {
        if (!padreId.HasValue)
        {
            return;
        }

        if (padreId == idActual)
        {
            throw new AppException(400, "CATALOGO_CICLICO", "Un catálogo no puede ser padre de sí mismo.");
        }

        var padre = await db.Catalogos.FindAsync([padreId.Value], cancellationToken)
            ?? throw new AppException(400, "CATALOGO_PADRE_INVALIDO", "El catálogo padre no existe.");
        if (!padre.Activo)
        {
            throw new AppException(400, "CATALOGO_PADRE_INACTIVO", "El catálogo padre está inactivo.");
        }

        var actual = padreId.Value;
        var visitados = new HashSet<Guid>();
        while (true)
        {
            if (idActual.HasValue && actual == idActual.Value)
            {
                throw new AppException(400, "CATALOGO_CICLICO", "La jerarquía de catálogos no puede tener ciclos.");
            }
            if (!visitados.Add(actual))
            {
                break;
            }
            var siguiente = await db.Catalogos
                .Where(x => x.Id == actual)
                .Select(x => x.CatalogoPadreId)
                .FirstOrDefaultAsync(cancellationToken);
            if (!siguiente.HasValue)
            {
                break;
            }
            actual = siguiente.Value;
        }
    }

    private async Task ValidarCodigoItemUnicoAsync(
        Guid catalogoId,
        string codigo,
        Guid? ignorarId,
        CancellationToken cancellationToken)
    {
        var normalizado = codigo.Trim().ToLowerInvariant();
        var existe = await db.CatalogoItems.AnyAsync(
            x => x.CatalogoId == catalogoId &&
                 x.Codigo.ToLower() == normalizado &&
                 (!ignorarId.HasValue || x.Id != ignorarId.Value),
            cancellationToken);
        if (existe)
        {
            throw new AppException(409, "ITEM_DUPLICADO", "Ya existe un ítem con ese código en el catálogo.");
        }
    }

    private async Task ValidarItemPadreAsync(
        Catalogo catalogo,
        Guid? padreItemId,
        Guid? ignorarId,
        CancellationToken cancellationToken)
    {
        if (!catalogo.CatalogoPadreId.HasValue)
        {
            if (padreItemId.HasValue)
            {
                var existe = await db.CatalogoItems.AnyAsync(
                    x => x.Id == padreItemId.Value &&
                         x.CatalogoId == catalogo.Id &&
                         x.Activo &&
                         (!ignorarId.HasValue || x.Id != ignorarId.Value),
                    cancellationToken);
                if (!existe)
                {
                    throw new AppException(400, "PADRE_ITEM_INVALIDO", "El ítem padre no existe en este catálogo.");
                }
            }
            return;
        }

        if (!padreItemId.HasValue)
        {
            throw new AppException(400, "PADRE_ITEM_REQUERIDO", "Los ítems de un catálogo dependiente requieren un padre.");
        }

        if (padreItemId == ignorarId)
        {
            throw new AppException(400, "PADRE_ITEM_INVALIDO", "Un ítem no puede ser padre de sí mismo.");
        }

        var padreValido = await db.CatalogoItems.AnyAsync(
            x => x.Id == padreItemId.Value &&
                 x.CatalogoId == catalogo.CatalogoPadreId.Value &&
                 x.Activo,
            cancellationToken);
        if (!padreValido)
        {
            throw new AppException(400, "PADRE_ITEM_INVALIDO", "El ítem padre no pertenece al catálogo padre.");
        }
    }

    private async Task<string?> ResolverPadreItemAsync(
        Catalogo catalogo,
        string? codigo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return catalogo.CatalogoPadreId.HasValue
                ? "falta PadreItemCodigo en un catálogo dependiente"
                : null;
        }

        var encontrado = await BuscarPadreItemIdAsync(catalogo, codigo, cancellationToken);
        return encontrado.HasValue
            ? null
            : $"PadreItemCodigo '{codigo}' no existe en el catálogo padre.";
    }

    private async Task<Guid?> BuscarPadreItemIdAsync(
        Catalogo catalogo,
        string? codigo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }

        var padreCatalogoId = catalogo.CatalogoPadreId ?? catalogo.Id;
        var normalizado = codigo.Trim().ToLowerInvariant();
        var item = await db.CatalogoItems.FirstOrDefaultAsync(
            x => x.CatalogoId == padreCatalogoId &&
                 x.Activo &&
                 (x.Codigo.ToLower() == normalizado || x.Id.ToString() == codigo.Trim()),
            cancellationToken);
        return item?.Id;
    }

    private static CatalogoDto Map(Catalogo catalogo) => new(
        catalogo.Id,
        catalogo.Nombre,
        catalogo.Descripcion,
        catalogo.Activo,
        catalogo.CatalogoPadreId,
        catalogo.Items.OrderBy(x => x.Orden).ThenBy(x => x.Texto).Select(MapItem).ToList());

    private static CatalogoItemDto MapItem(CatalogoItem item) => new(
        item.Id,
        item.CatalogoId,
        item.Codigo,
        item.Texto,
        item.Orden,
        item.Activo,
        item.PadreItemId);

    private sealed record FilaImportacion(
        int Numero,
        string Codigo,
        string Texto,
        int Orden,
        string PadreItemCodigo,
        string? Error);
}
