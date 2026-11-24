using GestionFormulario.Application.Common;
using GestionFormulario.Domain.Common;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService currentUser,
    TimeProvider timeProvider)
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    public DbSet<Formulario> Formularios => Set<Formulario>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Seccion> Secciones => Set<Seccion>();
    public DbSet<Campo> Campos => Set<Campo>();
    public DbSet<Catalogo> Catalogos => Set<Catalogo>();
    public DbSet<CatalogoItem> CatalogoItems => Set<CatalogoItem>();
    public DbSet<Respuesta> Respuestas => Set<Respuesta>();
    public DbSet<RespuestaValor> RespuestaValores => Set<RespuestaValor>();
    public DbSet<RespuestaValorHistorial> RespuestaValorHistoriales => Set<RespuestaValorHistorial>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();
    public DbSet<FormularioRol> FormulariosRoles => Set<FormularioRol>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditing();
        return base.SaveChanges();
    }

    private void ApplyAuditing()
    {
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var usuarioId = currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditable>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.FechaCreacion = ahora;
                    entry.Entity.CreadoPorId = usuarioId;
                    break;
                case EntityState.Modified:
                    entry.Entity.FechaModificacion = ahora;
                    entry.Entity.ModificadoPorId = usuarioId;
                    break;
            }
        }

        var entriesParaAuditar = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entriesParaAuditar)
        {
            var accion = entry.State switch
            {
                EntityState.Added => AccionAuditoria.Crear,
                EntityState.Modified => AccionAuditoria.Editar,
                _ => AccionAuditoria.Eliminar
            };

            var entidad = entry.Entity.GetType().Name;
            var idProp = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
            var entidadId = idProp is null
                ? string.Empty
                : entry.Property(idProp).CurrentValue?.ToString() ?? string.Empty;

            Auditorias.Add(new Auditoria
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Entidad = entidad,
                EntidadId = entidadId,
                Accion = accion,
                Fecha = ahora,
                Detalle = null
            });
        }
    }
}
