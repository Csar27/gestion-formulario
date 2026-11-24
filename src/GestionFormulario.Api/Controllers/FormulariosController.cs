using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Features.Formularios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/formularios")]
[Authorize]
public sealed class FormulariosController(IFormularioService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("Formularios.Ver")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("Formularios.Ver")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("Formularios.Crear")]
    public async Task<IActionResult> Create(
        [FromBody] FormularioCreateRequest request,
        CancellationToken cancellationToken)
    {
        var creado = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Formularios.Editar")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] FormularioUpdateRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission("Formularios.Eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
