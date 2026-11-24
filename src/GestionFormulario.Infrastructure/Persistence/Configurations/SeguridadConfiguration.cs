using GestionFormulario.Domain.Entities;
using GestionFormulario.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Modulo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Accion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Codigo).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasIndex(x => new { x.Modulo, x.Accion }).IsUnique();
    }
}

internal sealed class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolesPermisos");
        builder.HasKey(x => new { x.RolId, x.PermisoId });

        builder.HasOne(x => x.Permiso)
            .WithMany(x => x.RolesPermisos)
            .HasForeignKey(x => x.PermisoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FormularioRolConfiguration : IEntityTypeConfiguration<FormularioRol>
{
    public void Configure(EntityTypeBuilder<FormularioRol> builder)
    {
        builder.ToTable("FormulariosRoles");
        builder.HasKey(x => new { x.FormularioId, x.RolId });

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UsuarioId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UsuarioId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("Auditorias");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UsuarioId).HasMaxLength(450);
        builder.Property(x => x.Entidad).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntidadId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Accion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Fecha).IsRequired();
        builder.HasIndex(x => new { x.Entidad, x.EntidadId });
        builder.HasIndex(x => x.Fecha);
    }
}
