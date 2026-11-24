using GestionFormulario.Domain.Entities;

namespace GestionFormulario.Application.Features.Respuestas;

public sealed record ResultadoValidacion(string Valor, string? ValorTexto);

/// <summary>Valida y normaliza un valor según el tipo de campo.</summary>
public interface IValorCampoValidator
{
    Task<ResultadoValidacion> ValidarAsync(
        Campo campo,
        string? valor,
        bool obligatorio,
        string? valorPadre = null,
        CancellationToken cancellationToken = default);
}
