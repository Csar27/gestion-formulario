namespace GestionFormulario.Application.Features.Roles;

public sealed record PermisoDto(Guid Id, string Codigo, string Modulo, string Accion);

public sealed record RolDto(
    string Id,
    string Nombre,
    string? Descripcion,
    bool EsSistema,
    IReadOnlyCollection<string> Permisos);

public sealed record RolCreateRequest(
    string Nombre,
    string? Descripcion,
    IReadOnlyCollection<string>? Permisos);

public sealed record RolUpdateRequest(string? Descripcion);

public sealed record AsignarPermisosRequest(IReadOnlyCollection<string> Codigos);
