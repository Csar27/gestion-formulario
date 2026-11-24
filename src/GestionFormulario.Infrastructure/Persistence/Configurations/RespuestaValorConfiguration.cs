using GestionFormulario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal sealed class RespuestaValorConfiguration : IEntityTypeConfiguration<RespuestaValor>
{
    public void Configure(EntityTypeBuilder<RespuestaValor> builder)
    {
        builder.ToTable("RespuestaValores");
        builder.HasKey(x => new { x.RespuestaId, x.CampoId });
        builder.Property(x => x.Valor).IsRequired();
        builder.Property(x => x.ValorTexto);

        builder.HasOne(x => x.Respuesta)
            .WithMany(x => x.Valores)
            .HasForeignKey(x => x.RespuestaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Campo)
            .WithMany()
            .HasForeignKey(x => x.CampoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
