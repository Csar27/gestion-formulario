using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

public sealed class RespuestaValorHistorialConfiguration : IEntityTypeConfiguration<RespuestaValorHistorial>
{
    public void Configure(EntityTypeBuilder<RespuestaValorHistorial> builder)
    {
        builder.ToTable("RespuestaValorHistoriales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValorAnterior).HasMaxLength(4000);
        builder.Property(x => x.ValorAnteriorTexto).HasMaxLength(4000);
        builder.Property(x => x.ValorNuevo).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ValorNuevoTexto).HasMaxLength(4000);
        builder.Property(x => x.UsuarioId).IsRequired().HasMaxLength(450);

        builder.HasOne<Respuesta>()
            .WithMany()
            .HasForeignKey(x => x.RespuestaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Campo>()
            .WithMany()
            .HasForeignKey(x => x.CampoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}