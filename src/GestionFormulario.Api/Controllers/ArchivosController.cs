using GestionFormulario.Api.Authorization;
using GestionFormulario.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/archivos")]
[Authorize]
public sealed class ArchivosController(IWebHostEnvironment entorno) : ControllerBase
{
    [HttpPost]
    [HasPermission("Respuestas.Crear")]
    public async Task<IActionResult> Subir(IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new AppException(400, "ARCHIVO_VACIO", "El archivo está vacío.");
        }

        var raiz = entorno.WebRootPath;
        if (string.IsNullOrWhiteSpace(raiz))
        {
            raiz = Path.Combine(entorno.ContentRootPath, "wwwroot");
        }

        var carpeta = Path.Combine(raiz, "uploads", "archivos");
        Directory.CreateDirectory(carpeta);

        var extension = Path.GetExtension(archivo.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var nombre = $"{Guid.NewGuid()}{extension}";
        var ruta = Path.Combine(carpeta, nombre);

        await using var stream = System.IO.File.Create(ruta);
        await archivo.CopyToAsync(stream, cancellationToken);

        return Ok(new { url = $"/uploads/archivos/{nombre}" });
    }
}