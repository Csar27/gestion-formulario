using Microsoft.AspNetCore.Identity;

namespace GestionFormulario.Infrastructure.Identity;

public sealed class AppRole : IdentityRole
{
    public string? Descripcion { get; set; }
    /// <summary>Los roles de sistema (Admin) no se pueden editar ni eliminar.</summary>
    public bool EsSistema { get; set; }
}
