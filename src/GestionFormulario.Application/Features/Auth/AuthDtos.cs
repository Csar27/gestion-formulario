namespace GestionFormulario.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime Expira,
    bool DebeCambiarPassword,
    string UsuarioId,
    string NombreCompleto,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permisos);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

public sealed record OlvidePasswordRequest(string Email);

public sealed record OlvidePasswordResponse(bool TokenEntregado, string? Token);

public sealed record ResetPasswordRequest(string Email, string Token, string PasswordNueva);

public sealed record MisPermisosResponse(
    string UsuarioId,
    string Email,
    string NombreCompleto,
    bool DebeCambiarPassword,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permisos);

public sealed record OperacionResponse(bool Exitoso, string Mensaje);
