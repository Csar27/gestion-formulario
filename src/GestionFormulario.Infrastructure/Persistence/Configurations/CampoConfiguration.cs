using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class CampoConfiguration : IEntityTypeConfiguration<Campo>
{
    public void Configure(EntityTypeBuilder<Campo> builder)
    {
        builder.ToTable("Campos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Etiqueta).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Requerido).IsRequired();
        builder.Property(x => x.Orden).IsRequired();
        builder.Property(x => x.Min).HasColumnType("numeric(18,4)");
        builder.Property(x => x.Max).HasColumnType("numeric(18,4)");
        builder.Property(x => x.FuenteOpciones).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OpcionesJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.SeccionId, x.Orden });
        builder.HasIndex(x => x.CatalogoId);
        builder.HasIndex(x => x.CampoPadreId);

        builder.HasOne(x => x.Catalogo)
            .WithMany()
            .HasForeignKey(x => x.CatalogoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CampoPadre)
            .WithMany()
            .HasForeignKey(x => x.CampoPadreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureAuditable();
    }
}
