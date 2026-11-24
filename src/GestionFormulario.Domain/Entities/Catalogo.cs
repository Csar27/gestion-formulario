using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class Catalogo : AuditableEntity
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public Guid? CatalogoPadreId { get; set; }

    public Catalogo? CatalogoPadre { get; set; }
    public ICollection<Catalogo> CatalogosHijos { get; set; } = new List<Catalogo>();
    public ICollection<CatalogoItem> Items { get; set; } = new List<CatalogoItem>();
}
