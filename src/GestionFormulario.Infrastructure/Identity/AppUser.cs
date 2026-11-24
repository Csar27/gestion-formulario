using Microsoft.AspNetCore.Identity;

namespace GestionFormulario.Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool DebeCambiarPassword { get; set; }
    public DateTime FechaCreacion { get; set; }
}
