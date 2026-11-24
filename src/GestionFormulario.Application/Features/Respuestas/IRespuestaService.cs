using GestionFormulario.Application.Common;

namespace GestionFormulario.Application.Features.Respuestas;

public interface IRespuestaService
{
    Task<RespuestaDto> GuardarAsync(GuardarRespuestaRequest request, CancellationToken cancellationToken = default);
    Task<RespuestaDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<RespuestaListItemDto>> ListAsync(RespuestaListQuery query, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RespuestaValorHistorialDto>> GetHistorialAsync(Guid id, CancellationToken cancellationToken = default);
}
