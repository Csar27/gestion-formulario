using GestionFormulario.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.NombreCompleto).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Activo).IsRequired();
        builder.Property(x => x.DebeCambiarPassword).IsRequired();
        builder.Property(x => x.FechaCreacion).IsRequired();
        builder.HasIndex(x => x.Activo);
    }
}

internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.EsSistema).IsRequired();
    }
}
