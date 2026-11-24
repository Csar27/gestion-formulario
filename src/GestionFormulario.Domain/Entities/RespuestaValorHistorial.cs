using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class RespuestaValorHistorial : IAuditable
{
    public Guid Id { get; set; }
    public Guid RespuestaId { get; set; }
    public Guid CampoId { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorAnteriorTexto { get; set; }
    public string ValorNuevo { get; set; } = string.Empty;
    public string? ValorNuevoTexto { get; set; }
    public DateTime Fecha { get; set; }
    public string UsuarioId { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
    public string? CreadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPorId { get; set; }
}