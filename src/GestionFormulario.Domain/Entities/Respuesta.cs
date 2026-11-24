using GestionFormulario.Domain.Common;
using GestionFormulario.Domain.Enums;

namespace GestionFormulario.Domain.Entities;

public sealed class Respuesta : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid FormularioId { get; set; }
    public DateTime Fecha { get; set; }
    /// <summary>Id del usuario de Identity (string).</summary>
    public string UsuarioId { get; set; } = string.Empty;
    public EstadoRespuesta Estado { get; set; } = EstadoRespuesta.Borrador;

    public Formulario Formulario { get; set; } = null!;
    public ICollection<RespuestaValor> Valores { get; set; } = new List<RespuestaValor>();
}
