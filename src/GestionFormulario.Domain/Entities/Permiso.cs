using GestionFormulario.Domain.Enums;

namespace GestionFormulario.Domain.Entities;

public sealed class Permiso
{
    public Guid Id { get; set; }
    public Modulo Modulo { get; set; }
    public Accion Accion { get; set; }
    /// <summary>Código canónico "Modulo.Accion", por ejemplo "Catalogos.Editar".</summary>
    public string Codigo { get; set; } = string.Empty;

    public ICollection<RolPermiso> RolesPermisos { get; set; } = new List<RolPermiso>();

    public static string BuildCodigo(Modulo modulo, Accion accion) => $"{modulo}.{accion}";
}
