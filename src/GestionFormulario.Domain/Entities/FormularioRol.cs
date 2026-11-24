namespace GestionFormulario.Domain.Entities;

/// <summary>Permisos opcionales por formulario (qué roles pueden llenarlo o ver sus reportes).</summary>
public sealed class FormularioRol
{
    public Guid FormularioId { get; set; }
    public string RolId { get; set; } = string.Empty;
    public bool PuedeLlenar { get; set; }
    public bool PuedeVerReportes { get; set; }

    public Formulario Formulario { get; set; } = null!;
}
