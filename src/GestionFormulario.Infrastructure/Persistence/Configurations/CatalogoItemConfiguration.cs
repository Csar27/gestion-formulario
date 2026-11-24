using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class CatalogoItemConfiguration : IEntityTypeConfiguration<CatalogoItem>
{
    public void Configure(EntityTypeBuilder<CatalogoItem> builder)
    {
        builder.ToTable("CatalogoItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Texto).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Orden).IsRequired();
        builder.Property(x => x.Activo).IsRequired();

        builder.HasIndex(x => new { x.CatalogoId, x.Codigo }).IsUnique();
        builder.HasIndex(x => new { x.CatalogoId, x.PadreItemId, x.Activo, x.Orden });

        builder.HasOne(x => x.PadreItem)
            .WithMany(x => x.ItemsHijos)
            .HasForeignKey(x => x.PadreItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureAuditable();
    }
}
