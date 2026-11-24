using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Respuestas;

public sealed record ValorRequest(Guid CampoId, string? Valor);

public sealed record GuardarRespuestaRequest(
    Guid? Id,
    Guid FormularioId,
    Guid? SeccionId,
    IReadOnlyList<ValorRequest> Valores,
    bool Finalizar);

public sealed record RespuestaValorDto(Guid CampoId, string Etiqueta, string Valor, string? ValorTexto);

public sealed record RespuestaDto(
    Guid Id,
    Guid FormularioId,
    string FormularioNombre,
    DateTime Fecha,
    string UsuarioId,
    string Estado,
    IReadOnlyList<RespuestaValorDto> Valores);

public sealed record RespuestaListItemDto(
    Guid Id,
    Guid FormularioId,
    string FormularioNombre,
    DateTime Fecha,
    string Estado,
    int CantidadValores,
    string UsuarioId);

public sealed record RespuestaValorHistorialDto(
    Guid Id,
    Guid CampoId,
    string Etiqueta,
    string? ValorAnterior,
    string? ValorAnteriorTexto,
    string ValorNuevo,
    string? ValorNuevoTexto,
    DateTime Fecha,
    string UsuarioId);

public sealed record RespuestaListQuery(
    Guid? FormularioId,
    DateTime? Desde,
    DateTime? Hasta,
    string? Estado,
    int Page = 1,
    int PageSize = 20);
