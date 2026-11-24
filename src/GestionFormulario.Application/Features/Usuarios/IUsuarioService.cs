using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Usuarios;

public interface IUsuarioService
{
    Task<PagedResult<UsuarioDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UsuarioDto> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<UsuarioDto> CreateAsync(UsuarioCreateRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioDto> UpdateAsync(string id, UsuarioUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
