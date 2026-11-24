using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class SeccionConfiguration : IEntityTypeConfiguration<Seccion>
{
    public void Configure(EntityTypeBuilder<Seccion> builder)
    {
        builder.ToTable("Secciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Orden).IsRequired();
        builder.HasIndex(x => new { x.FormularioId, x.Orden });

        builder.HasMany(x => x.Campos)
            .WithOne(x => x.Seccion)
            .HasForeignKey(x => x.SeccionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditable();
    }
}
