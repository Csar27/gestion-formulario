using GestionFormulario.Domain.Common;
using GestionFormulario.Domain.Enums;

namespace GestionFormulario.Domain.Entities;

public sealed class Campo : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid SeccionId { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public TipoCampo Tipo { get; set; }
    public bool Requerido { get; set; }
    public int Orden { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    public FuenteOpciones FuenteOpciones { get; set; }
    /// <summary>JSON con opciones manuales: [{ "valor": "...", "etiqueta": "..." }]. Solo cuando FuenteOpciones = Manual.</summary>
    public string? OpcionesJson { get; set; }
    public Guid? CatalogoId { get; set; }
    /// <summary>Campo del mismo formulario del que depende este campo cuando el catálogo es dependiente.</summary>
    public Guid? CampoPadreId { get; set; }

    public Seccion Seccion { get; set; } = null!;
    public Catalogo? Catalogo { get; set; }
    public Campo? CampoPadre { get; set; }
}
