namespace GestionFormulario.Application.Features.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task CambiarPasswordAsync(string usuarioId, CambiarPasswordRequest request, CancellationToken cancellationToken = default);
    Task<OlvidePasswordResponse> OlvidePasswordAsync(OlvidePasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<MisPermisosResponse> MisPermisosAsync(string usuarioId, CancellationToken cancellationToken = default);
}
