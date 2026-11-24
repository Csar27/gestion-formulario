using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Productos;

public sealed record ProductoDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    string? ImagenUrl,
    Guid? FormularioId,
    string? FormularioNombre);

public sealed record ProductoCreateRequest(
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? FormularioId);

public sealed record ProductoUpdateRequest(
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? FormularioId);