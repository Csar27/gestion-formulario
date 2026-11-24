using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Roles;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Infrastructure.Identity;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionFormulario.Infrastructure.Services;

public sealed class RolService(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    AppDbContext db,
    IValidator<RolCreateRequest> createValidator,
    IValidator<RolUpdateRequest> updateValidator,
    IValidator<AsignarPermisosRequest> permisosValidator) : IRolService
{
    public async Task<IReadOnlyList<RolDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var permisosPorRol = await ObtenerPermisosPorRolAsync(cancellationToken);
        return roles.Select(r => Map(r, permisosPorRol)).ToList();
    }

    public async Task<RolDto> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var rol = await roleManager.FindByIdAsync(id)
            ?? throw new AppException(404, "ROL_NO_ENCONTRADO", "El rol no existe.");
        return new RolDto(
            rol.Id,
            rol.Name ?? string.Empty,
            rol.Descripcion,
            rol.EsSistema,
            await GetPermisosAsync(rol.Id, cancellationToken));
    }

    public async Task<RolDto> CreateAsync(RolCreateRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await roleManager.FindByNameAsync(request.Nombre) is not null)
        {
            throw new AppException(409, "ROL_DUPLICADO", "Ya existe un rol con ese nombre.");
        }

        var rol = new AppRole
        {
            Name = request.Nombre,
            NormalizedName = request.Nombre.ToUpperInvariant(),
            Descripcion = request.Descripcion,
            EsSistema = false
        };

        var resultado = await roleManager.CreateAsync(rol);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "ROL_RECHAZADO", DescribirErrores(resultado.Errors));
        }

        await ReemplazarPermisosAsync(rol.Id, request.Permisos ?? Array.Empty<string>(), cancellationToken);
        return await GetAsync(rol.Id, cancellationToken);
    }

    public async Task<RolDto> UpdateAsync(
        string id,
        RolUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var rol = await roleManager.FindByIdAsync(id)
            ?? throw new AppException(404, "ROL_NO_ENCONTRADO", "El rol no existe.");
        if (rol.EsSistema)
        {
            throw new AppException(400, "ROL_SISTEMA", "Los roles del sistema no se pueden editar.");
        }

        rol.Descripcion = request.Descripcion;
        var resultado = await roleManager.UpdateAsync(rol);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "ROL_RECHAZADO", DescribirErrores(resultado.Errors));
        }

        return await GetAsync(rol.Id, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var rol = await roleManager.FindByIdAsync(id)
            ?? throw new AppException(404, "ROL_NO_ENCONTRADO", "El rol no existe.");
        if (rol.EsSistema)
        {
            throw new AppException(400, "ROL_SISTEMA", "Los roles del sistema no se pueden eliminar.");
        }

        var usuarios = await userManager.GetUsersInRoleAsync(rol.Name!);
        if (usuarios.Count > 0)
        {
            throw new AppException(409, "ROL_EN_USO", "El rol tiene usuarios asignados.");
        }

        var resultado = await roleManager.DeleteAsync(rol);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "ROL_RECHAZADO", DescribirErrores(resultado.Errors));
        }
    }

    public async Task<IReadOnlyList<string>> GetPermisosAsync(string id, CancellationToken cancellationToken = default)
    {
        return await (from rp in db.RolesPermisos
                      join p in db.Permisos on rp.PermisoId equals p.Id
                      where rp.RolId == id
                      select p.Codigo)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task SetPermisosAsync(
        string id,
        AsignarPermisosRequest request,
        CancellationToken cancellationToken = default)
    {
        await permisosValidator.ValidateAndThrowAsync(request, cancellationToken);

        var rol = await roleManager.FindByIdAsync(id)
            ?? throw new AppException(404, "ROL_NO_ENCONTRADO", "El rol no existe.");
        if (rol.EsSistema)
        {
            throw new AppException(400, "ROL_SISTEMA", "Los permisos del rol Admin no se pueden modificar.");
        }

        await ReemplazarPermisosAsync(rol.Id, request.Codigos, cancellationToken);
    }

    public async Task<IReadOnlyList<PermisoDto>> ListPermisosAsync(CancellationToken cancellationToken = default)
    {
        return await db.Permisos
            .OrderBy(x => x.Modulo)
            .ThenBy(x => x.Accion)
            .Select(x => new PermisoDto(x.Id, x.Codigo, x.Modulo.ToString(), x.Accion.ToString()))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, IReadOnlyList<string>>> ObtenerPermisosPorRolAsync(
        CancellationToken cancellationToken)
    {
        var filas = await (from rp in db.RolesPermisos
                           join p in db.Permisos on rp.PermisoId equals p.Id
                           select new { rp.RolId, p.Codigo })
            .ToListAsync(cancellationToken);

        return filas
            .GroupBy(x => x.RolId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Codigo).OrderBy(x => x).ToList());
    }

    private async Task ReemplazarPermisosAsync(
        string rolId,
        IReadOnlyCollection<string> codigos,
        CancellationToken cancellationToken)
    {
        var distintos = codigos.Distinct().ToList();
        var encontrados = await db.Permisos
            .Where(x => distintos.Contains(x.Codigo))
            .ToListAsync(cancellationToken);

        if (encontrados.Count != distintos.Count)
        {
            var invalidos = distintos.Except(encontrados.Select(x => x.Codigo));
            throw new AppException(400, "PERMISO_INVALIDO", $"Códigos de permiso no válidos: {string.Join(", ", invalidos)}");
        }

        var actuales = await db.RolesPermisos
            .Where(x => x.RolId == rolId)
            .ToListAsync(cancellationToken);
        db.RolesPermisos.RemoveRange(actuales);

        foreach (var permiso in encontrados)
        {
            db.RolesPermisos.Add(new RolPermiso { RolId = rolId, PermisoId = permiso.Id });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RolDto Map(
        AppRole rol,
        Dictionary<string, IReadOnlyList<string>> permisosPorRol) =>
        new(
            rol.Id,
            rol.Name ?? string.Empty,
            rol.Descripcion,
            rol.EsSistema,
            permisosPorRol.TryGetValue(rol.Id, out var permisos) ? permisos : Array.Empty<string>());

    private static string DescribirErrores(IEnumerable<IdentityError> errores) =>
        string.Join(" ", errores.Select(e => e.Description));
}
