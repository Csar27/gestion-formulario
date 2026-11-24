namespace GestionFormulario.Domain.Common;

/// <summary>
/// Contrato de auditoría. El usuario se guarda como string (Id de ASP.NET Core Identity).
/// </summary>
public interface IAuditable
{
    DateTime FechaCreacion { get; set; }
    string? CreadoPorId { get; set; }
    DateTime? FechaModificacion { get; set; }
    string? ModificadoPorId { get; set; }
}

public abstract class AuditableEntity : IAuditable
{
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPorId { get; set; }
}
