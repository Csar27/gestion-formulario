namespace GestionFormulario.Application.Features.Catalogos;

public sealed record CatalogoItemDto(
    Guid Id,
    Guid CatalogoId,
    string Codigo,
    string Texto,
    int Orden,
    bool Activo,
    Guid? PadreItemId);

public sealed record CatalogoDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? CatalogoPadreId,
    IReadOnlyList<CatalogoItemDto> Items);

public sealed record CatalogoResumenDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? CatalogoPadreId,
    int CantidadItems);

public sealed record CatalogoCreateRequest(
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? CatalogoPadreId);

public sealed record CatalogoUpdateRequest(
    string Nombre,
    string? Descripcion,
    bool Activo,
    Guid? CatalogoPadreId);

public sealed record CatalogoItemCreateRequest(
    string Codigo,
    string Texto,
    int Orden,
    bool Activo,
    Guid? PadreItemId);

public sealed record CatalogoItemUpdateRequest(
    string Codigo,
    string Texto,
    int Orden,
    bool Activo,
    Guid? PadreItemId);

public sealed record OpcionCatalogoDto(Guid Id, string Codigo, string Texto);

public sealed record ImportarCatalogoRequest(Stream Contenido, string NombreArchivo, bool Reemplazar);

public sealed record ImportacionResultadoDto(
    int Procesadas,
    int Creadas,
    int Actualizadas,
    int Desactivadas,
    IReadOnlyList<string> Errores);
