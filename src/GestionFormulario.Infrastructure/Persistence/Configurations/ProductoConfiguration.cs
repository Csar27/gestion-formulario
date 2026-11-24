using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Descripcion).HasMaxLength(1000);

        builder.HasOne(x => x.Formulario)
            .WithMany()
            .HasForeignKey(x => x.FormularioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}