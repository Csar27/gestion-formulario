using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Formularios;

public sealed record OpcionDto(string Valor, string Etiqueta);

public sealed record CampoDto(
    Guid Id,
    Guid SeccionId,
    string Etiqueta,
    string Tipo,
    bool Requerido,
    int Orden,
    decimal? Min,
    decimal? Max,
    string FuenteOpciones,
    IReadOnlyList<OpcionDto> Opciones,
    Guid? CatalogoId,
    Guid? CampoPadreId);

public sealed record SeccionDto(
    Guid Id,
    Guid FormularioId,
    string Titulo,
    int Orden,
    IReadOnlyList<CampoDto> Campos);

public sealed record FormularioDto(
    Guid Id,
    string Nombre,
    bool EsPasoAPaso,
    IReadOnlyList<SeccionDto> Secciones,
    IReadOnlyList<string> Roles);

public sealed record CampoRequest(
    string Etiqueta,
    string Tipo,
    bool Requerido,
    int Orden,
    decimal? Min,
    decimal? Max,
    string FuenteOpciones,
    IReadOnlyList<OpcionDto>? Opciones,
    Guid? CatalogoId,
    Guid? CampoPadreId);

public sealed record SeccionRequest(
    string Titulo,
    int Orden,
    IReadOnlyList<CampoRequest> Campos);

public sealed record FormularioCreateRequest(
    string Nombre,
    bool EsPasoAPaso,
    IReadOnlyList<SeccionRequest> Secciones,
    IReadOnlyList<string>? Roles);

public sealed record FormularioUpdateRequest(
    string Nombre,
    bool EsPasoAPaso,
    IReadOnlyList<SeccionRequest> Secciones,
    IReadOnlyList<string>? Roles);
