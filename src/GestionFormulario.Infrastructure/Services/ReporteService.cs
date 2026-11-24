using ClosedXML.Excel;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Reportes;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionFormulario.Infrastructure.Services;

public sealed class ReporteService(
    AppDbContext db,
    ICurrentUserService currentUser) : IReporteService
{
    static ReporteService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<PagedResult<ReporteRespuestaDto>> ListAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default)
    {
        var consulta = CrearConsulta(query);
        var total = await consulta.CountAsync(cancellationToken);

        var pagina = Math.Max(1, query.Page);
        var tamano = Math.Clamp(query.PageSize, 1, 100);

        var respuestas = await consulta
            .OrderByDescending(x => x.Fecha)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        var usuarios = await ObtenerUsuariosAsync(respuestas.Select(x => x.UsuarioId), cancellationToken);
        var items = respuestas.Select(x => Mapear(x, usuarios)).ToList();

        return new PagedResult<ReporteRespuestaDto>(items, total, pagina, tamano);
    }

    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarExcelAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default)
    {
        var respuestas = await CrearConsulta(query)
            .OrderByDescending(x => x.Fecha)
            .ToListAsync(cancellationToken);

        var usuarios = await ObtenerUsuariosAsync(respuestas.Select(x => x.UsuarioId), cancellationToken);
        var reportes = respuestas.Select(x => Mapear(x, usuarios)).ToList();
        var columnas = await ObtenerColumnasAsync(query.FormularioId, reportes, cancellationToken);

        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Reporte");

        var encabezados = new List<string> { "Formulario", "Fecha", "Usuario", "Estado" };
        encabezados.AddRange(columnas.Select(x => x.Encabezado));

        for (var i = 0; i < encabezados.Count; i++)
        {
            var celda = hoja.Cell(1, i + 1);
            celda.Value = encabezados[i];
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F3864");
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var fila = 2;
        foreach (var respuesta in reportes)
        {
            var porCampo = respuesta.Valores.ToDictionary(x => x.CampoId, x => x.ValorMostrado);
            hoja.Cell(fila, 1).Value = respuesta.FormularioNombre;
            hoja.Cell(fila, 2).Value = respuesta.Fecha.ToString("dd/MM/yyyy HH:mm");
            hoja.Cell(fila, 3).Value = respuesta.UsuarioNombre;
            hoja.Cell(fila, 4).Value = respuesta.Estado;

            var columna = 5;
            foreach (var (campoId, _) in columnas)
            {
                hoja.Cell(fila, columna++).Value =
                    porCampo.TryGetValue(campoId, out var valor) ? valor : string.Empty;
            }

            for (var c = 1; c <= encabezados.Count; c++)
            {
                hoja.Cell(fila, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            fila++;
        }

        hoja.Columns().AdjustToContents();
        hoja.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (stream.ToArray(), $"ReporteRespuestas_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarPdfAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default)
    {
        var respuestas = await CrearConsulta(query)
            .OrderByDescending(x => x.Fecha)
            .ToListAsync(cancellationToken);

        var usuarios = await ObtenerUsuariosAsync(respuestas.Select(x => x.UsuarioId), cancellationToken);
        var reportes = respuestas.Select(x => Mapear(x, usuarios)).ToList();
        var columnas = await ObtenerColumnasAsync(query.FormularioId, reportes, cancellationToken);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(columna =>
                {
                    columna.Item().Text("Reporte de respuestas").SemiBold().FontSize(16).FontColor("#1F3864");
                    columna.Item().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor("#595959");
                    columna.Item().Text($"Registros: {reportes.Count}").FontSize(8).FontColor("#595959");
                });

                page.Content().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnasDefinicion =>
                    {
                        columnasDefinicion.RelativeColumn(2);
                        columnasDefinicion.RelativeColumn(1.3f);
                        columnasDefinicion.RelativeColumn(1.8f);
                        columnasDefinicion.RelativeColumn(1);
                        foreach (var _ in columnas)
                        {
                            columnasDefinicion.RelativeColumn(1.2f);
                        }
                    });

                    tabla.Header(encabezado =>
                    {
                        encabezado.Cell().Text("Formulario").FontColor("#FFFFFF").SemiBold().FontSize(9).BackgroundColor("#1F3864");
                        encabezado.Cell().Text("Fecha").FontColor("#FFFFFF").SemiBold().FontSize(9).BackgroundColor("#1F3864");
                        encabezado.Cell().Text("Usuario").FontColor("#FFFFFF").SemiBold().FontSize(9).BackgroundColor("#1F3864");
                        encabezado.Cell().Text("Estado").FontColor("#FFFFFF").SemiBold().FontSize(9).BackgroundColor("#1F3864");
                        foreach (var (_, titulo) in columnas)
                        {
                            encabezado.Cell().Text(titulo).FontColor("#FFFFFF").SemiBold().FontSize(9).BackgroundColor("#1F3864");
                        }
                    });

                    foreach (var respuesta in reportes)
                    {
                        var porCampo = respuesta.Valores.ToDictionary(x => x.CampoId, x => x.ValorMostrado);
                        tabla.Cell().Text(respuesta.FormularioNombre);
                        tabla.Cell().Text(respuesta.Fecha.ToString("dd/MM/yyyy HH:mm"));
                        tabla.Cell().Text(respuesta.UsuarioNombre);
                        tabla.Cell().Text(respuesta.Estado);

                        foreach (var (campoId, _) in columnas)
                        {
                            tabla.Cell().Text(porCampo.TryGetValue(campoId, out var valor) ? valor : string.Empty);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        });

        return (documento.GeneratePdf(), $"ReporteRespuestas_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    public async Task<DashboardResumenDto> ObtenerDashboardAsync(
        Guid? formularioId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        string? usuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var filtro = db.Respuestas.AsNoTracking().Include(x => x.Formulario).AsQueryable();
        if (!currentUser.EsAdmin)
        {
            var actual = currentUser.UserId ?? throw new AppException(401, "NO_AUTENTICADO", "Se requiere autenticación.");
            filtro = filtro.Where(x => x.UsuarioId == actual);
        }
        else if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            filtro = filtro.Where(x => x.UsuarioId == usuarioId);
        }

        if (formularioId.HasValue)
        {
            filtro = filtro.Where(x => x.FormularioId == formularioId.Value);
        }

        if (desde.HasValue)
        {
            filtro = filtro.Where(x => x.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            var limite = hasta.Value.Date.AddDays(1);
            filtro = filtro.Where(x => x.Fecha < limite);
        }

        var respuestas = await filtro.ToListAsync(cancellationToken);
        var total = respuestas.Count;
        var borradores = respuestas.Count(x => x.Estado == EstadoRespuesta.Borrador);
        var completas = respuestas.Count(x => x.Estado == EstadoRespuesta.Completa);

        var porFormulario = respuestas
            .GroupBy(x => new { x.FormularioId, Nombre = x.Formulario != null ? x.Formulario.Nombre : string.Empty })
            .Select(g => new DashboardFormularioDto(
                g.Key.FormularioId,
                g.Key.Nombre,
                g.Count(),
                g.Count(x => x.Estado == EstadoRespuesta.Borrador),
                g.Count(x => x.Estado == EstadoRespuesta.Completa)))
            .OrderBy(x => x.Nombre)
            .ToList();

        return new DashboardResumenDto(total, borradores, completas, porFormulario);
    }

    private IQueryable<Respuesta> CrearConsulta(ReporteRespuestaQuery query)
    {
        var filtro = db.Respuestas
            .AsNoTracking()
            .Include(x => x.Formulario)
            .Include(x => x.Valores).ThenInclude(x => x.Campo)
            .AsQueryable();

        if (!currentUser.EsAdmin)
        {
            var usuarioId = currentUser.UserId
                ?? throw new AppException(401, "NO_AUTENTICADO", "Se requiere autenticación.");
            filtro = filtro.Where(x => x.UsuarioId == usuarioId);
        }
        else if (!string.IsNullOrWhiteSpace(query.UsuarioId))
        {
            filtro = filtro.Where(x => x.UsuarioId == query.UsuarioId);
        }

        if (query.FormularioId.HasValue)
        {
            filtro = filtro.Where(x => x.FormularioId == query.FormularioId.Value);
        }

        if (query.Desde.HasValue)
        {
            filtro = filtro.Where(x => x.Fecha >= query.Desde.Value);
        }

        if (query.Hasta.HasValue)
        {
            var limite = query.Hasta.Value.Date.AddDays(1);
            filtro = filtro.Where(x => x.Fecha < limite);
        }

        if (!string.IsNullOrWhiteSpace(query.Estado) &&
            Enum.TryParse<EstadoRespuesta>(query.Estado, ignoreCase: true, out var estado))
        {
            filtro = filtro.Where(x => x.Estado == estado);
        }

        return filtro;
    }

    private async Task<Dictionary<string, string>> ObtenerUsuariosAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return await db.Users
            .AsNoTracking()
            .Where(x => lista.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.NombreCompleto, cancellationToken);
    }

    private static ReporteRespuestaDto Mapear(Respuesta respuesta, Dictionary<string, string> usuarios)
    {
        var valores = respuesta.Valores
            .OrderBy(x => x.Campo?.Orden)
            .Select(x => new ReporteValorDto(
                x.CampoId,
                x.Campo?.Etiqueta ?? string.Empty,
                x.Valor,
                x.ValorTexto,
                string.IsNullOrWhiteSpace(x.ValorTexto) ? x.Valor : x.ValorTexto))
            .ToList();

        return new ReporteRespuestaDto(
            respuesta.Id,
            respuesta.FormularioId,
            respuesta.Formulario?.Nombre ?? string.Empty,
            respuesta.Fecha,
            respuesta.UsuarioId,
            usuarios.TryGetValue(respuesta.UsuarioId, out var nombre) ? nombre : respuesta.UsuarioId,
            respuesta.Estado.ToString(),
            valores.Count,
            valores);
    }

    private async Task<List<(Guid CampoId, string Encabezado)>> ObtenerColumnasAsync(
        Guid? formularioId,
        IReadOnlyList<ReporteRespuestaDto> reportes,
        CancellationToken cancellationToken)
    {
        if (formularioId.HasValue)
        {
            var formulario = await db.Formularios
                .AsNoTracking()
                .Include(x => x.Secciones).ThenInclude(x => x.Campos)
                .FirstOrDefaultAsync(x => x.Id == formularioId.Value, cancellationToken);

            if (formulario is not null)
            {
                return formulario.Secciones
                    .OrderBy(x => x.Orden)
                    .SelectMany(x => x.Campos.OrderBy(c => c.Orden))
                    .Select(x => (x.Id, x.Etiqueta))
                    .ToList();
            }
        }

        return reportes
            .SelectMany(x => x.Valores)
            .GroupBy(x => x.CampoId)
            .Select(x => (x.Key, $"{x.First().Etiqueta}"))
            .ToList();
    }

}