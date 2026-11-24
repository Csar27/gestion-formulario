using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Reportes;

public interface IReporteService
{
    Task<PagedResult<ReporteRespuestaDto>> ListAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default);

    Task<(byte[] Contenido, string NombreArchivo)> ExportarExcelAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default);

    Task<(byte[] Contenido, string NombreArchivo)> ExportarPdfAsync(
        ReporteRespuestaQuery query,
        CancellationToken cancellationToken = default);

    Task<DashboardResumenDto> ObtenerDashboardAsync(
        Guid? formularioId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        string? usuarioId = null,
        CancellationToken cancellationToken = default);
}