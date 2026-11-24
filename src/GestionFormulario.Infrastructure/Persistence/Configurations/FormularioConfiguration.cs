using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class FormularioConfiguration : IEntityTypeConfiguration<Formulario>
{
    public void Configure(EntityTypeBuilder<Formulario> builder)
    {
        builder.ToTable("Formularios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.EsPasoAPaso).IsRequired();

        builder.HasMany(x => x.Secciones)
            .WithOne(x => x.Formulario)
            .HasForeignKey(x => x.FormularioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FormulariosRoles)
            .WithOne(x => x.Formulario)
            .HasForeignKey(x => x.FormularioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditable();
    }
}
