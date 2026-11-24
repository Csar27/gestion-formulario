using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Features.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(IRolService service) : ControllerBase
{
    [HttpGet("permisos-disponibles")]
    [HasPermission("Roles.Ver")]
    public async Task<IActionResult> PermisosDisponibles(CancellationToken cancellationToken)
        => Ok(await service.ListPermisosAsync(cancellationToken));

    [HttpGet]
    [HasPermission("Roles.Ver")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{id}")]
    [HasPermission("Roles.Ver")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("Roles.Crear")]
    public async Task<IActionResult> Create([FromBody] RolCreateRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id}")]
    [HasPermission("Roles.Editar")]
    public async Task<IActionResult> Update(string id, [FromBody] RolUpdateRequest request, CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [HasPermission("Roles.Eliminar")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/permisos")]
    [HasPermission("Roles.Ver")]
    public async Task<IActionResult> GetPermisos(string id, CancellationToken cancellationToken)
        => Ok(await service.GetPermisosAsync(id, cancellationToken));

    [HttpPut("{id}/permisos")]
    [HasPermission("Roles.Editar")]
    public async Task<IActionResult> SetPermisos(
        string id,
        [FromBody] AsignarPermisosRequest request,
        CancellationToken cancellationToken)
    {
        await service.SetPermisosAsync(id, request, cancellationToken);
        return Ok(new { rolId = id, permisos = await service.GetPermisosAsync(id, cancellationToken) });
    }
}
