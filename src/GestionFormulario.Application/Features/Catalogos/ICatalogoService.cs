namespace GestionFormulario.Application.Features.Catalogos;

public interface ICatalogoService
{
    Task<IReadOnlyList<CatalogoResumenDto>> ListAsync(bool soloActivos = true, CancellationToken cancellationToken = default);
    Task<CatalogoDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CatalogoDto> CreateAsync(CatalogoCreateRequest request, CancellationToken cancellationToken = default);
    Task<CatalogoDto> UpdateAsync(Guid id, CatalogoUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpcionCatalogoDto>> ObtenerOpcionesAsync(
        Guid catalogoId,
        Guid? padreItemId,
        string? buscar,
        bool soloActivos = true,
        CancellationToken cancellationToken = default);

    Task<CatalogoItemDto> CreateItemAsync(Guid catalogoId, CatalogoItemCreateRequest request, CancellationToken cancellationToken = default);
    Task<CatalogoItemDto> UpdateItemAsync(Guid itemId, CatalogoItemUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ImportacionResultadoDto> ImportarAsync(Guid catalogoId, ImportarCatalogoRequest request, CancellationToken cancellationToken = default);
}
