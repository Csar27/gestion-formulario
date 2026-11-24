using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Productos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController(IProductoService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("Productos.Ver")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await service.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("Productos.Ver")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("Productos.Crear")]
    public async Task<IActionResult> Create([FromBody] ProductoCreateRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission("Productos.Editar")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductoUpdateRequest request, CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission("Productos.Eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/imagen")]
    [HasPermission("Productos.Editar")]
    public async Task<IActionResult> Imagen(Guid id, [FromForm] IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new AppException(400, "ARCHIVO_VACIO", "El archivo está vacío.");
        }

        await using var contenido = archivo.OpenReadStream();
        var producto = await service.GuardarImagenAsync(id, contenido, archivo.FileName, cancellationToken);
        return Ok(producto);
    }
}