using GestionFormulario.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionFormulario.Infrastructure.Persistence.Configurations;

internal static class AuditableConfiguration
{
    public static void ConfigureAuditable<T>(this EntityTypeBuilder<T> builder) where T : class, IAuditable
    {
        builder.Property(x => x.FechaCreacion).IsRequired();
        builder.Property(x => x.CreadoPorId).HasMaxLength(450);
        builder.Property(x => x.FechaModificacion);
        builder.Property(x => x.ModificadoPorId).HasMaxLength(450);
    }
}
