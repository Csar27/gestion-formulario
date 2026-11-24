using GestionFormulario.Domain.Enums;

namespace GestionFormulario.Domain.Entities;

public sealed class Auditoria
{
    public Guid Id { get; set; }
    public string? UsuarioId { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public AccionAuditoria Accion { get; set; }
    public DateTime Fecha { get; set; }
    public string? Detalle { get; set; }
}
