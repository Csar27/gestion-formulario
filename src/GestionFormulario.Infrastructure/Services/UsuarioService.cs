using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Usuarios;
using GestionFormulario.Infrastructure.Identity;
using GestionFormulario.Infrastructure.Persistence;
using GestionFormulario.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionFormulario.Infrastructure.Services;

public sealed class UsuarioService(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    IValidator<UsuarioCreateRequest> createValidator,
    IValidator<UsuarioUpdateRequest> updateValidator,
    IOptions<SeedOptions> seedOptions,
    TimeProvider timeProvider) : IUsuarioService
{
    public async Task<PagedResult<UsuarioDto>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var total = await db.Users.CountAsync(cancellationToken);
        var usuarios = await db.Users
            .OrderBy(x => x.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var roles = await ObtenerRolesPorUsuarioAsync(cancellationToken);

        var items = usuarios.Select(u => Map(u, roles)).ToList();
        return new PagedResult<UsuarioDto>(items, total, page, pageSize);
    }

    public async Task<UsuarioDto> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(id)
            ?? throw new AppException(404, "USUARIO_NO_ENCONTRADO", "El usuario no existe.");
        var roles = await userManager.GetRolesAsync(usuario);
        return Map(usuario, roles);
    }

    public async Task<UsuarioDto> CreateAsync(
        UsuarioCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new AppException(409, "EMAIL_DUPLICADO", "Ya existe un usuario con ese correo.");
        }

        var usuario = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            NombreCompleto = request.NombreCompleto,
            Activo = true,
            DebeCambiarPassword = true,
            LockoutEnabled = true,
            FechaCreacion = timeProvider.GetUtcNow().UtcDateTime
        };

        var resultado = await userManager.CreateAsync(usuario, request.Password);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "USUARIO_RECHAZADO", DescribirErrores(resultado.Errors));
        }

        await ReemplazarRolesAsync(usuario, request.Roles ?? Array.Empty<string>());
        return Map(usuario, await userManager.GetRolesAsync(usuario));
    }

    public async Task<UsuarioDto> UpdateAsync(
        string id,
        UsuarioUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManager.FindByIdAsync(id)
            ?? throw new AppException(404, "USUARIO_NO_ENCONTRADO", "El usuario no existe.");

        var otro = await userManager.FindByEmailAsync(request.Email);
        if (otro is not null && otro.Id != usuario.Id)
        {
            throw new AppException(409, "EMAIL_DUPLICADO", "Ya existe un usuario con ese correo.");
        }

        usuario.Email = request.Email;
        usuario.UserName = request.Email;
        usuario.NombreCompleto = request.NombreCompleto;
        usuario.Activo = request.Activo;

        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "USUARIO_RECHAZADO", DescribirErrores(resultado.Errors));
        }

        if (request.Roles is not null)
        {
            await ReemplazarRolesAsync(usuario, request.Roles);
        }

        return Map(usuario, await userManager.GetRolesAsync(usuario));
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(id)
            ?? throw new AppException(404, "USUARIO_NO_ENCONTRADO", "El usuario no existe.");

        var roles = await userManager.GetRolesAsync(usuario);
        if (roles.Contains(seedOptions.Value.AdminRoleName))
        {
            throw new AppException(400, "ADMIN_NO_ELIMINABLE", "El usuario administrador no se puede eliminar.");
        }

        usuario.Activo = false;
        await userManager.UpdateAsync(usuario);

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var tokens = await db.RefreshTokens
            .Where(x => x.UsuarioId == usuario.Id && !x.Revocado)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.Revocado = true;
            token.FechaRevocacion = ahora;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, IList<string>>> ObtenerRolesPorUsuarioAsync(
        CancellationToken cancellationToken)
    {
        var filas = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.Id
                           select new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        return filas
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => x.Name!).ToList());
    }

    private async Task ReemplazarRolesAsync(AppUser usuario, IReadOnlyCollection<string> nombres)
    {
        var rolesValidos = new List<string>();
        foreach (var nombre in nombres.Distinct())
        {
            if (await roleManager.FindByNameAsync(nombre) is null)
            {
                throw new AppException(400, "ROL_NO_ENCONTRADO", $"El rol '{nombre}' no existe.");
            }
            rolesValidos.Add(nombre);
        }

        var actuales = await userManager.GetRolesAsync(usuario);
        if (actuales.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(usuario, actuales);
        }
        if (rolesValidos.Count > 0)
        {
            await userManager.AddToRolesAsync(usuario, rolesValidos);
        }
    }

    private static UsuarioDto Map(
        AppUser usuario,
        IList<string> roles) => new(
            usuario.Id,
            usuario.Email ?? string.Empty,
            usuario.NombreCompleto,
            usuario.Activo,
            usuario.DebeCambiarPassword,
            roles);

    private static UsuarioDto Map(
        AppUser usuario,
        Dictionary<string, IList<string>> rolesPorUsuario) =>
        Map(usuario, rolesPorUsuario.TryGetValue(usuario.Id, out var roles) ? roles : Array.Empty<string>());

    private static string DescribirErrores(IEnumerable<IdentityError> errores) =>
        string.Join(" ", errores.Select(e => e.Description));
}
