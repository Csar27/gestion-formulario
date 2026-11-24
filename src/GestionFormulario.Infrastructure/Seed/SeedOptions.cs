namespace GestionFormulario.Infrastructure.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = "admin@gestionformulario.local";
    public string AdminPassword { get; set; } = "Admin123!";
    public string AdminNombreCompleto { get; set; } = "Administrador";
    public string AdminRoleName { get; set; } = "Admin";
}
