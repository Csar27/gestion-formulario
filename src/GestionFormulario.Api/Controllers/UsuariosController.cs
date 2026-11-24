using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Features.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuariosController(IUsuarioService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("Usuarios.Ver")]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{id}")]
    [HasPermission("Usuarios.Ver")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("Usuarios.Crear")]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id}")]
    [HasPermission("Usuarios.Editar")]
    public async Task<IActionResult> Update(string id, [FromBody] UsuarioUpdateRequest request, CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    [HasPermission("Usuarios.Eliminar")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
