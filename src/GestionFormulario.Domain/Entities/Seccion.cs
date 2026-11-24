using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class Seccion : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid FormularioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int Orden { get; set; }

    public Formulario Formulario { get; set; } = null!;
    public ICollection<Campo> Campos { get; set; } = new List<Campo>();
}
