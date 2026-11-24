using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Formularios;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Services;

public sealed class FormularioService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IValidator<FormularioCreateRequest> createValidator,
    IValidator<FormularioUpdateRequest> updateValidator) : IFormularioService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<FormularioDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = currentUser.Roles.ToList();
        var query = db.Formularios
            .AsNoTracking()
            .Include(x => x.Secciones).ThenInclude(x => x.Campos)
            .Include(x => x.FormulariosRoles)
            .AsQueryable();

        if (!currentUser.EsAdmin)
        {
            query = query.Where(x =>
                !x.FormulariosRoles.Any(r => r.PuedeLlenar) ||
                x.FormulariosRoles.Any(r => r.PuedeLlenar && roles.Contains(r.RolId)));
        }

        var formularios = await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        return formularios.Select(Map).ToList();
    }

    public async Task<FormularioDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var formulario = await db.Formularios
            .AsNoTracking()
            .Include(x => x.Secciones).ThenInclude(x => x.Campos)
            .Include(x => x.FormulariosRoles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "FORMULARIO_NO_ENCONTRADO", "El formulario no existe.");

        return Map(formulario);
    }

    public async Task<FormularioDto> CreateAsync(
        FormularioCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await ValidarNombreUnicoAsync(request.Nombre, null, cancellationToken);
        await ValidarRolesAsync(request.Roles, cancellationToken);

        var formulario = new Formulario
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            EsPasoAPaso = request.EsPasoAPaso
        };

        ReemplazarSecciones(formulario, request.Secciones);
        ReemplazarRoles(formulario, request.Roles);

        db.Formularios.Add(formulario);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(formulario.Id, cancellationToken);
    }

    public async Task<FormularioDto> UpdateAsync(
        Guid id,
        FormularioUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var formulario = await db.Formularios
            .Include(x => x.Secciones).ThenInclude(x => x.Campos)
            .Include(x => x.FormulariosRoles)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "FORMULARIO_NO_ENCONTRADO", "El formulario no existe.");

        await ValidarNombreUnicoAsync(request.Nombre, id, cancellationToken);
        await ValidarRolesAsync(request.Roles, cancellationToken);

        formulario.Nombre = request.Nombre.Trim();
        formulario.EsPasoAPaso = request.EsPasoAPaso;
        SincronizarSecciones(formulario, request.Secciones);

        foreach (var rol in formulario.FormulariosRoles.ToList())
        {
            db.FormulariosRoles.Remove(rol);
        }
        formulario.FormulariosRoles.Clear();
        ReemplazarRoles(formulario, request.Roles);

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var formulario = await db.Formularios.FindAsync([id], cancellationToken)
            ?? throw new AppException(404, "FORMULARIO_NO_ENCONTRADO", "El formulario no existe.");

        if (await db.Respuestas.AnyAsync(x => x.FormularioId == id, cancellationToken))
        {
            throw new AppException(409, "FORMULARIO_EN_USO", "El formulario tiene respuestas guardadas.");
        }

        db.Formularios.Remove(formulario);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidarNombreUnicoAsync(
        string nombre,
        Guid? ignorarId,
        CancellationToken cancellationToken)
    {
        var normalizado = nombre.Trim().ToLowerInvariant();
        var existe = await db.Formularios.AnyAsync(
            x => x.Nombre.ToLower() == normalizado && (!ignorarId.HasValue || x.Id != ignorarId.Value),
            cancellationToken);

        if (existe)
        {
            throw new AppException(409, "FORMULARIO_DUPLICADO", "Ya existe un formulario con ese nombre.");
        }
    }

    private async Task ValidarRolesAsync(
        IReadOnlyList<string>? roles,
        CancellationToken cancellationToken)
    {
        var ids = roles?.Distinct().ToList() ?? new List<string>();
        if (ids.Count == 0)
        {
            return;
        }

        var encontrados = await db.Roles.CountAsync(x => ids.Contains(x.Id), cancellationToken);
        if (encontrados != ids.Count)
        {
            throw new AppException(400, "ROL_INVALIDO", "Uno de los roles indicados no existe.");
        }
    }

    private void SincronizarSecciones(Formulario formulario, IReadOnlyList<SeccionRequest> solicitud)
    {
        var existentes = formulario.Secciones.OrderBy(x => x.Orden).ToList();
        var solicitadas = solicitud.OrderBy(x => x.Orden).ToList();
        var camposEliminados = new HashSet<Guid>();

        for (var i = 0; i < solicitadas.Count; i++)
        {
            var requerimiento = solicitadas[i];
            var seccion = i < existentes.Count
                ? existentes[i]
                : new Seccion { Id = Guid.NewGuid(), FormularioId = formulario.Id };
            if (i >= existentes.Count)
            {
                db.Secciones.Add(seccion);
            }

            seccion.Titulo = requerimiento.Titulo.Trim();
            seccion.Orden = requerimiento.Orden;

            var camposExistentes = seccion.Campos.OrderBy(x => x.Orden).ToList();
            var camposSolicitados = requerimiento.Campos.OrderBy(x => x.Orden).ToList();

            for (var j = 0; j < camposSolicitados.Count; j++)
            {
                var campo = j < camposExistentes.Count
                    ? camposExistentes[j]
                    : new Campo { Id = Guid.NewGuid(), SeccionId = seccion.Id };
                if (j >= camposExistentes.Count)
                {
                    db.Campos.Add(campo);
                }

                AplicarCampo(campo, camposSolicitados[j]);
            }

            foreach (var extra in camposExistentes.Skip(camposSolicitados.Count))
            {
                camposEliminados.Add(extra.Id);
                seccion.Campos.Remove(extra);
            }
        }

        foreach (var extra in existentes.Skip(solicitadas.Count))
        {
            foreach (var campo in extra.Campos)
            {
                camposEliminados.Add(campo.Id);
            }
            formulario.Secciones.Remove(extra);
        }

        foreach (var campo in formulario.Secciones.SelectMany(x => x.Campos))
        {
            if (campo.CampoPadreId.HasValue && camposEliminados.Contains(campo.CampoPadreId.Value))
            {
                campo.CampoPadreId = null;
            }
        }
    }

    private static void AplicarCampo(Campo campo, CampoRequest campoRequest)
    {
        campo.Etiqueta = campoRequest.Etiqueta.Trim();
        campo.Tipo = Enum.Parse<TipoCampo>(campoRequest.Tipo, ignoreCase: true);
        campo.Requerido = campoRequest.Requerido;
        campo.Orden = campoRequest.Orden;
        campo.Min = campoRequest.Min;
        campo.Max = campoRequest.Max;
        campo.FuenteOpciones = Enum.Parse<FuenteOpciones>(campoRequest.FuenteOpciones, ignoreCase: true);
        campo.OpcionesJson = SerializarOpciones(campoRequest);
        campo.CatalogoId = campoRequest.CatalogoId;
        campo.CampoPadreId = campoRequest.CampoPadreId;
    }

    private static void ReemplazarSecciones(Formulario formulario, IReadOnlyList<SeccionRequest> secciones)
    {
        foreach (var seccionRequest in secciones)
        {
            var seccion = new Seccion
            {
                Id = Guid.NewGuid(),
                FormularioId = formulario.Id,
                Titulo = seccionRequest.Titulo.Trim(),
                Orden = seccionRequest.Orden
            };

            foreach (var campoRequest in seccionRequest.Campos)
            {
                var campo = new Campo { Id = Guid.NewGuid(), SeccionId = seccion.Id };
                AplicarCampo(campo, campoRequest);
                seccion.Campos.Add(campo);
            }

            formulario.Secciones.Add(seccion);
        }
    }

    private static void ReemplazarRoles(Formulario formulario, IReadOnlyList<string>? roles)
    {
        foreach (var rolId in roles?.Distinct() ?? Array.Empty<string>())
        {
            formulario.FormulariosRoles.Add(new FormularioRol
            {
                FormularioId = formulario.Id,
                RolId = rolId,
                PuedeLlenar = true,
                PuedeVerReportes = true
            });
        }
    }

    private static string? SerializarOpciones(CampoRequest campo)
    {
        var tipo = Enum.Parse<TipoCampo>(campo.Tipo, ignoreCase: true);
        var fuente = Enum.Parse<FuenteOpciones>(campo.FuenteOpciones, ignoreCase: true);
        if (tipo != TipoCampo.Lista || fuente != FuenteOpciones.Manual || campo.Opciones is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            campo.Opciones.Select(x => new OpcionJson { Valor = x.Valor, Etiqueta = x.Etiqueta }),
            JsonOptions);
    }

    private static FormularioDto Map(Formulario formulario) => new(
        formulario.Id,
        formulario.Nombre,
        formulario.EsPasoAPaso,
        formulario.Secciones.OrderBy(x => x.Orden).Select(MapSeccion).ToList(),
        formulario.FormulariosRoles.Select(x => x.RolId).OrderBy(x => x).ToList());

    private static SeccionDto MapSeccion(Seccion seccion) => new(
        seccion.Id,
        seccion.FormularioId,
        seccion.Titulo,
        seccion.Orden,
        seccion.Campos.OrderBy(x => x.Orden).Select(MapCampo).ToList());

    private static CampoDto MapCampo(Campo campo) => new(
        campo.Id,
        campo.SeccionId,
        campo.Etiqueta,
        campo.Tipo.ToString(),
        campo.Requerido,
        campo.Orden,
        campo.Min,
        campo.Max,
        campo.FuenteOpciones.ToString(),
        DeserializarOpciones(campo.OpcionesJson),
        campo.CatalogoId,
        campo.CampoPadreId);

    private static IReadOnlyList<OpcionDto> DeserializarOpciones(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<OpcionDto>();
        }

        var opciones = JsonSerializer.Deserialize<List<OpcionJson>>(json, JsonOptions) ?? new List<OpcionJson>();
        return opciones.Select(x => new OpcionDto(x.Valor, x.Etiqueta)).ToList();
    }

    private sealed class OpcionJson
    {
        [JsonPropertyName("valor")]
        public string Valor { get; set; } = string.Empty;

        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; set; } = string.Empty;
    }
}
