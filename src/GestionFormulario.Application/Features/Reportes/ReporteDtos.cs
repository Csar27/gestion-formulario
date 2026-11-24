using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Reportes;

public sealed record ReporteRespuestaQuery(
    Guid? FormularioId,
    DateTime? Desde,
    DateTime? Hasta,
    string? Estado,
    string? UsuarioId,
    int Page = 1,
    int PageSize = 20);

public sealed record ReporteValorDto(
    Guid CampoId,
    string Etiqueta,
    string Valor,
    string? ValorTexto,
    string ValorMostrado);

public sealed record ReporteRespuestaDto(
    Guid Id,
    Guid FormularioId,
    string FormularioNombre,
    DateTime Fecha,
    string UsuarioId,
    string UsuarioNombre,
    string Estado,
    int CantidadValores,
    IReadOnlyList<ReporteValorDto> Valores);