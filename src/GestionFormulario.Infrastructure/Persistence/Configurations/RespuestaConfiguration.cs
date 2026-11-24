using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class RespuestaConfiguration : IEntityTypeConfiguration<Respuesta>
{
    public void Configure(EntityTypeBuilder<Respuesta> builder)
    {
        builder.ToTable("Respuestas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Fecha).IsRequired();
        builder.Property(x => x.UsuarioId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => new { x.FormularioId, x.Fecha });
        builder.HasIndex(x => new { x.UsuarioId, x.Fecha });

        builder.HasOne(x => x.Formulario)
            .WithMany()
            .HasForeignKey(x => x.FormularioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Valores)
            .WithOne(x => x.Respuesta)
            .HasForeignKey(x => x.RespuestaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditable();
    }
}
