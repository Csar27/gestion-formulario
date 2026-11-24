using GestionFormulario.Domain.Common;

namespace GestionFormulario.Domain.Entities;

public sealed class CatalogoItem : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid CatalogoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    /// <summary>Ítem del catálogo padre al que pertenece este ítem (catálogos dependientes).</summary>
    public Guid? PadreItemId { get; set; }

    public Catalogo Catalogo { get; set; } = null!;
    public CatalogoItem? PadreItem { get; set; }
    public ICollection<CatalogoItem> ItemsHijos { get; set; } = new List<CatalogoItem>();
}
