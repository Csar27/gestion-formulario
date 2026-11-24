namespace GestionFormulario.Domain.Entities;

/// <summary>Relación entre un rol de Identity y un permiso. RolId es el Id string del rol.</summary>
public sealed class RolPermiso
{
    public string RolId { get; set; } = string.Empty;
    public Guid PermisoId { get; set; }

    public Permiso Permiso { get; set; } = null!;
}
