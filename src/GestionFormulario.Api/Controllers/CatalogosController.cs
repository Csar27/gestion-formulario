using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Catalogos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/catalogos")]
[Authorize]
public sealed class CatalogosController(ICatalogoService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("Catalogos.Ver")]
    public async Task<IActionResult> List([FromQuery] bool soloActivos = true, CancellationToken cancellationToken = default)
        => Ok(await service.ListAsync(soloActivos, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("Catalogos.Ver")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("Catalogos.Crear")]
    public async Task<IActionResult> Create(
        [FromBody] CatalogoCreateRequest request,
        CancellationToken cancellationToken)
    {
        var creado = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("Catalogos.Editar")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CatalogoUpdateRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission("Catalogos.Eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/items")]
    [HasPermission("Catalogos.Ver")]
    public async Task<IActionResult> Opciones(
        Guid id,
        [FromQuery] Guid? padreItemId,
        [FromQuery] string? buscar,
        [FromQuery] bool soloActivos = true,
        CancellationToken cancellationToken = default)
        => Ok(await service.ObtenerOpcionesAsync(id, padreItemId, buscar, soloActivos, cancellationToken));

    [HttpPost("{catalogoId:guid}/items")]
    [HasPermission("Catalogos.Editar")]
    public async Task<IActionResult> CreateItem(
        Guid catalogoId,
        [FromBody] CatalogoItemCreateRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.CreateItemAsync(catalogoId, request, cancellationToken));

    [HttpPut("items/{itemId:guid}")]
    [HasPermission("Catalogos.Editar")]
    public async Task<IActionResult> UpdateItem(
        Guid itemId,
        [FromBody] CatalogoItemUpdateRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateItemAsync(itemId, request, cancellationToken));

    [HttpDelete("items/{itemId:guid}")]
    [HasPermission("Catalogos.Eliminar")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        await service.DeleteItemAsync(itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/importar")]
    [HasPermission("Catalogos.Editar")]
    public async Task<IActionResult> Importar(
        Guid id,
        IFormFile archivo,
        [FromForm] bool reemplazar = false,
        CancellationToken cancellationToken = default)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new AppException(400, "ARCHIVO_VACIO", "El archivo está vacío.");
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await service.ImportarAsync(
            id,
            new ImportarCatalogoRequest(contenido, Path.GetFileName(archivo.FileName), reemplazar),
            cancellationToken);
        return Ok(resultado);
    }
}
