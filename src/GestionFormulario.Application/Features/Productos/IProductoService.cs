using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Productos;

public interface IProductoService
{
    Task<IReadOnlyList<ProductoDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProductoDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductoDto> CreateAsync(ProductoCreateRequest request, CancellationToken cancellationToken = default);
    Task<ProductoDto> UpdateAsync(Guid id, ProductoUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductoDto> GuardarImagenAsync(Guid id, Stream contenido, string nombreArchivo, CancellationToken cancellationToken = default);
}