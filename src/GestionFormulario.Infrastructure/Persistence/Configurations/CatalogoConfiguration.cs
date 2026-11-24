using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class CatalogoConfiguration : IEntityTypeConfiguration<Catalogo>
{
    public void Configure(EntityTypeBuilder<Catalogo> builder)
    {
        builder.ToTable("Catalogos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Descripcion).HasMaxLength(1000);
        builder.Property(x => x.Activo).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.HasIndex(x => x.CatalogoPadreId);
        builder.HasIndex(x => x.Activo);

        builder.HasOne(x => x.CatalogoPadre)
            .WithMany(x => x.CatalogosHijos)
            .HasForeignKey(x => x.CatalogoPadreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Catalogo)
            .HasForeignKey(x => x.CatalogoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditable();
    }
}
