using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Productos;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Hosting;

namespace GestionFormulario.Infrastructure.Services;

public sealed class ProductoService(AppDbContext db, IWebHostEnvironment entorno) : IProductoService
{
    public async Task<IReadOnlyList<ProductoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Productos
            .AsNoTracking()
            .Include(x => x.Formulario)
            .OrderBy(x => x.Nombre)
            .Select(x => new ProductoDto(x.Id, x.Nombre, x.Descripcion, x.Activo, x.ImagenUrl, x.FormularioId, x.Formulario != null ? x.Formulario.Nombre : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductoDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var producto = await db.Productos
            .AsNoTracking()
            .Include(x => x.Formulario)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "PRODUCTO_NO_ENCONTRADO", "El producto no existe.");

        return new ProductoDto(producto.Id, producto.Nombre, producto.Descripcion, producto.Activo, producto.ImagenUrl, producto.FormularioId, producto.Formulario?.Nombre);
    }

    public async Task<ProductoDto> CreateAsync(ProductoCreateRequest request, CancellationToken cancellationToken = default)
    {
        var producto = new Producto
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Activo = request.Activo,
            FormularioId = request.FormularioId
        };
        db.Productos.Add(producto);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(producto.Id, cancellationToken);
    }

    public async Task<ProductoDto> UpdateAsync(Guid id, ProductoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "PRODUCTO_NO_ENCONTRADO", "El producto no existe.");

        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = request.Descripcion?.Trim();
        producto.Activo = request.Activo;
        producto.FormularioId = request.FormularioId;

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "PRODUCTO_NO_ENCONTRADO", "El producto no existe.");

        db.Productos.Remove(producto);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductoDto> GuardarImagenAsync(Guid id, Stream contenido, string nombreArchivo, CancellationToken cancellationToken = default)
    {
        var producto = await db.Productos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppException(404, "PRODUCTO_NO_ENCONTRADO", "El producto no existe.");

        var raiz = entorno.WebRootPath;
        if (string.IsNullOrWhiteSpace(raiz))
        {
            raiz = Path.Combine(entorno.ContentRootPath, "wwwroot");
        }

        var carpeta = Path.Combine(raiz, "uploads", "productos");
        Directory.CreateDirectory(carpeta);

        var extension = Path.GetExtension(nombreArchivo);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var nombre = $"{id}{extension}";
        var ruta = Path.Combine(carpeta, nombre);

        await using var archivo = File.Create(ruta);
        await contenido.CopyToAsync(archivo, cancellationToken);

        producto.ImagenUrl = $"/uploads/productos/{nombre}";
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }
}