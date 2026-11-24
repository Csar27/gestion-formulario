namespace GestionFormulario.Application.Features.Formularios;

public interface IFormularioService
{
    Task<IReadOnlyList<FormularioDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<FormularioDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FormularioDto> CreateAsync(FormularioCreateRequest request, CancellationToken cancellationToken = default);
    Task<FormularioDto> UpdateAsync(Guid id, FormularioUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
