using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class Formulario : AuditableEntity
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsPasoAPaso { get; set; }

    public ICollection<Seccion> Secciones { get; set; } = new List<Seccion>();
    public ICollection<FormularioRol> FormulariosRoles { get; set; } = new List<FormularioRol>();
}
