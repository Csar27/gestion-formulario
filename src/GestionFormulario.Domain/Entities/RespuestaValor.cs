namespace GestionFormulario.Domain.Entities;

/// <summary>
/// Valor de un campo en una respuesta. Clave compuesta (RespuestaId, CampoId).
/// ValorTexto guarda una instantánea del texto del ítem de catálogo para que el histórico
/// no cambie si el ítem se edita o se da de baja.
/// </summary>
public sealed class RespuestaValor
{
    public Guid RespuestaId { get; set; }
    public Guid CampoId { get; set; }
    public string Valor { get; set; } = string.Empty;
    public string? ValorTexto { get; set; }

    public Respuesta Respuesta { get; set; } = null!;
    public Campo Campo { get; set; } = null!;
}
