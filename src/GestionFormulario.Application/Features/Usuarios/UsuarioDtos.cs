namespace GestionFormulario.Application.Features.Usuarios;

public sealed record UsuarioDto(
    string Id,
    string Email,
    string NombreCompleto,
    bool Activo,
    bool DebeCambiarPassword,
    IList<string> Roles);

public sealed record UsuarioCreateRequest(
    string Email,
    string NombreCompleto,
    string Password,
    IReadOnlyCollection<string>? Roles);

public sealed record UsuarioUpdateRequest(
    string Email,
    string NombreCompleto,
    bool Activo,
    IReadOnlyCollection<string>? Roles);
