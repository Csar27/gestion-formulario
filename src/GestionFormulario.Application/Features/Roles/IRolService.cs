namespace GestionFormulario.Application.Features.Roles;

public interface IRolService
{
    Task<IReadOnlyList<RolDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<RolDto> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<RolDto> CreateAsync(RolCreateRequest request, CancellationToken cancellationToken = default);
    Task<RolDto> UpdateAsync(string id, RolUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermisosAsync(string id, CancellationToken cancellationToken = default);
    Task SetPermisosAsync(string id, AsignarPermisosRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermisoDto>> ListPermisosAsync(CancellationToken cancellationToken = default);
}
