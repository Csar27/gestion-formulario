namespace GestionFormulario.Application.Features.Reportes;

public sealed record DashboardResumenDto(
    int TotalRespuestas,
    int Borradores,
    int Completas,
    IReadOnlyList<DashboardFormularioDto> PorFormulario);

public sealed record DashboardFormularioDto(
    Guid FormularioId,
    string Nombre,
    int Total,
    int Borradores,
    int Completas);