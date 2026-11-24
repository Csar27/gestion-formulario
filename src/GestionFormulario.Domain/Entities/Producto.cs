using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class Producto : IAuditable
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenUrl { get; set; }
    public Guid? FormularioId { get; set; }
    public Formulario? Formulario { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string? CreadoPorId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPorId { get; set; }
}