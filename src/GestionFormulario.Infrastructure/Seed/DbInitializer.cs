using GestionFormulario.Domain.Entities;
using GestionFormulario.Domain.Enums;
using GestionFormulario.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GestionFormulario.Infrastructure.Persistence;

namespace GestionFormulario.Infrastructure.Seed;

/// <summary>
/// Datos mínimos necesarios para arrancar: 30 permisos, rol Admin con todos y usuario administrador.
/// Idempotente: se puede ejecutar en cada arranque sin duplicar registros.
/// </summary>
public sealed class DbInitializer(
    AppDbContext db,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IOptions<SeedOptions> seedOptions,
    TimeProvider timeProvider,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        var permisos = await SeedPermisosAsync(cancellationToken);
        var rolAdmin = await SeedRolAdminAsync(permisos, cancellationToken);
        await SeedUsuarioAdminAsync(rolAdmin, cancellationToken);
    }

    private async Task<List<Permiso>> SeedPermisosAsync(CancellationToken cancellationToken)
    {
        var existentes = await db.Permisos.ToListAsync(cancellationToken);
        var existentesPorCodigo = existentes.ToDictionary(x => x.Codigo);
        var todos = new List<Permiso>();

        foreach (var moder in Enum.GetValues<Modulo>())
        foreach (var accion in Enum.GetValues<Accion>())
        {
            var codigo = Permiso.BuildCodigo(moder, accion);
            if (existentesPorCodigo.TryGetValue(codigo, out var permiso))
            {
                todos.Add(permiso);
                continue;
            }

            permiso = new Permiso
            {
                Id = Guid.NewGuid(),
                Modulo = moder,
                Accion = accion,
                Codigo = codigo
            };
            db.Permisos.Add(permiso);
            todos.Add(permiso);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Permisos sembrados: {Count}", todos.Count);
        return todos;
    }

    private async Task<AppRole> SeedRolAdminAsync(IReadOnlyCollection<Permiso> permisos, CancellationToken cancellationToken)
    {
        var opciones = seedOptions.Value;
        var rol = await roleManager.FindByNameAsync(opciones.AdminRoleName);
        if (rol is null)
        {
            rol = new AppRole
            {
                Name = opciones.AdminRoleName,
                NormalizedName = opciones.AdminRoleName.ToUpperInvariant(),
                Descripcion = "Administrador del sistema",
                EsSistema = true
            };
            var resultado = await roleManager.CreateAsync(rol);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el rol Admin: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
            }
        }
        else if (!rol.EsSistema)
        {
            rol.EsSistema = true;
            await roleManager.UpdateAsync(rol);
        }

        var asignados = await db.RolesPermisos
            .Where(x => x.RolId == rol.Id.ToString())
            .Select(x => x.PermisoId)
            .ToListAsync(cancellationToken);

        var faltantes = permisos.Where(p => !asignados.Contains(p.Id)).ToList();
        foreach (var permiso in faltantes)
        {
            db.RolesPermisos.Add(new RolPermiso { RolId = rol.Id.ToString(), PermisoId = permiso.Id });
        }

        if (faltantes.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Rol {Rol} con {Count} permisos", rol.Name, permisos.Count);
        return rol;
    }

    private async Task SeedUsuarioAdminAsync(AppRole rol, CancellationToken cancellationToken)
    {
        var opciones = seedOptions.Value;
        var usuario = await userManager.FindByEmailAsync(opciones.AdminEmail);
        if (usuario is null)
        {
            usuario = new AppUser
            {
                UserName = opciones.AdminEmail,
                Email = opciones.AdminEmail,
                EmailConfirmed = true,
                NombreCompleto = opciones.AdminNombreCompleto,
                Activo = true,
                DebeCambiarPassword = true,
                FechaCreacion = timeProvider.GetUtcNow().UtcDateTime
            };

            var resultado = await userManager.CreateAsync(usuario, opciones.AdminPassword);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario admin: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
            }
        }

        var roles = await userManager.GetRolesAsync(usuario);
        if (!roles.Contains(opciones.AdminRoleName))
        {
            var resultado = await userManager.AddToRoleAsync(usuario, opciones.AdminRoleName);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo asignar el rol Admin: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
            }
        }

        logger.LogInformation("Usuario admin verificado: {Email}", usuario.Email);
    }
}
