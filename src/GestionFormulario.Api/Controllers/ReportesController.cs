using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Features.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public sealed class ReportesController(IReporteService service) : ControllerBase
{
    [HttpGet("dashboard")]
    [HasPermission("Reportes.Ver")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? formularioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? usuarioId,
        CancellationToken cancellationToken = default)
        => Ok(await service.ObtenerDashboardAsync(formularioId, desde, hasta, usuarioId, cancellationToken));

    [HttpGet]
    [HasPermission("Reportes.Ver")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? formularioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? estado,
        [FromQuery] string? usuarioId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await service.ListAsync(
            new ReporteRespuestaQuery(formularioId, desde, hasta, estado, usuarioId, page, pageSize),
            cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("excel")]
    [HasPermission("Reportes.Exportar")]
    public async Task<IActionResult> Excel(
        [FromQuery] Guid? formularioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? estado,
        [FromQuery] string? usuarioId,
        CancellationToken cancellationToken = default)
    {
        var (contenido, nombre) = await service.ExportarExcelAsync(
            new ReporteRespuestaQuery(formularioId, desde, hasta, estado, usuarioId),
            cancellationToken);
        return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    [HttpGet("pdf")]
    [HasPermission("Reportes.Exportar")]
    public async Task<IActionResult> Pdf(
        [FromQuery] Guid? formularioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? estado,
        [FromQuery] string? usuarioId,
        CancellationToken cancellationToken = default)
    {
        var (contenido, nombre) = await service.ExportarPdfAsync(
            new ReporteRespuestaQuery(formularioId, desde, hasta, estado, usuarioId),
            cancellationToken);
        return File(contenido, "application/pdf", nombre);
    }
}