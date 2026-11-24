using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Features.Respuestas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/respuestas")]
[Authorize]
public sealed class RespuestasController(IRespuestaService service) : ControllerBase
{
    [HttpPost]
    [HasPermission("Respuestas.Crear")]
    public async Task<IActionResult> Guardar(
        [FromBody] GuardarRespuestaRequest request,
        CancellationToken cancellationToken)
    {
        var respuesta = await service.GuardarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = respuesta.Id }, respuesta);
    }

    [HttpGet]
    [HasPermission("Respuestas.Ver")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? formularioId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await service.ListAsync(
            new RespuestaListQuery(formularioId, desde, hasta, estado, page, pageSize),
            cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("Respuestas.Ver")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/historial")]
    [HasPermission("Respuestas.Ver")]
    public async Task<IActionResult> Historial(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetHistorialAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission("Respuestas.Eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
