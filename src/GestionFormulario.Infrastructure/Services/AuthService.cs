using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Auth;
using GestionFormulario.Domain.Entities;
using GestionFormulario.Infrastructure.Configuration;
using GestionFormulario.Infrastructure.Identity;
using GestionFormulario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionFormulario.Infrastructure.Services;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IJwtTokenGenerator tokenGenerator,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    IHostEnvironment environment,
    ILogger<AuthService> logger,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<CambiarPasswordRequest> cambiarPasswordValidator,
    IValidator<OlvidePasswordRequest> olvidePasswordValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManager.FindByEmailAsync(request.Email);
        if (usuario is null || !usuario.Activo)
        {
            throw new AppException(401, "CREDENCIALES_INVALIDAS", "Correo o contraseña incorrectos.");
        }

        if (await userManager.IsLockedOutAsync(usuario))
        {
            throw new AppException(423, "CUENTA_BLOQUEADA", "La cuenta está bloqueada. Inténtalo más tarde.");
        }

        if (!await userManager.CheckPasswordAsync(usuario, request.Password))
        {
            await userManager.AccessFailedAsync(usuario);
            if (await userManager.IsLockedOutAsync(usuario))
            {
                throw new AppException(423, "CUENTA_BLOQUEADA", "La cuenta está bloqueada. Inténtalo más tarde.");
            }
            throw new AppException(401, "CREDENCIALES_INVALIDAS", "Correo o contraseña incorrectos.");
        }

        await userManager.ResetAccessFailedCountAsync(usuario);

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var (refresh, entidadRefresh) = CrearRefreshToken(usuario.Id, ahora);
        db.RefreshTokens.Add(entidadRefresh);
        await db.SaveChangesAsync(cancellationToken);

        return await ConstruirLoginResponseAsync(usuario, refresh, cancellationToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var hash = HashToken(request.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            throw new AppException(401, "REFRESH_INVALIDO", "El token de actualización no es válido.");
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        if (token.Revocado)
        {
            // Reutilizar un token ya rotado indica robo: se revoca toda la familia del usuario.
            if (token.ReemplazadoPor.HasValue)
            {
                var activos = await db.RefreshTokens
                    .Where(x => x.UsuarioId == token.UsuarioId && !x.Revocado)
                    .ToListAsync(cancellationToken);
                foreach (var activo in activos)
                {
                    activo.Revocado = true;
                    activo.FechaRevocacion = ahora;
                }
                await db.SaveChangesAsync(cancellationToken);
            }
            throw new AppException(401, "REFRESH_INVALIDO", "El token de actualización ya fue utilizado.");
        }

        if (token.Expira <= ahora)
        {
            throw new AppException(401, "REFRESH_EXPIRADO", "El token de actualización ha expirado.");
        }

        var usuario = await userManager.FindByIdAsync(token.UsuarioId);
        if (usuario is null || !usuario.Activo)
        {
            throw new AppException(401, "REFRESH_INVALIDO", "El usuario no está activo.");
        }

        var (refresh, nuevoToken) = CrearRefreshToken(usuario.Id, ahora);
        db.RefreshTokens.Add(nuevoToken);
        token.Revocado = true;
        token.FechaRevocacion = ahora;
        token.ReemplazadoPor = nuevoToken.Id;
        await db.SaveChangesAsync(cancellationToken);

        return await ConstruirLoginResponseAsync(usuario, refresh, cancellationToken);
    }

    public async Task LogoutAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens
            .Where(x => x.UsuarioId == usuarioId && !x.Revocado)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var token in tokens)
        {
            token.Revocado = true;
            token.FechaRevocacion = ahora;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CambiarPasswordAsync(
        string usuarioId,
        CambiarPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await cambiarPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManager.FindByIdAsync(usuarioId)
            ?? throw new AppException(401, "USUARIO_NO_ENCONTRADO", "Usuario no autenticado.");

        if (!await userManager.CheckPasswordAsync(usuario, request.PasswordActual))
        {
            throw new AppException(400, "PASSWORD_ACTUAL_INVALIDA", "La contraseña actual no es correcta.");
        }

        var resultado = await userManager.ChangePasswordAsync(usuario, request.PasswordActual, request.PasswordNueva);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "PASSWORD_RECHAZADA", DescribirErrores(resultado.Errors));
        }

        usuario.DebeCambiarPassword = false;
        await userManager.UpdateAsync(usuario);
        await RevocarTokensAsync(usuario.Id, cancellationToken);
    }

    public async Task<OlvidePasswordResponse> OlvidePasswordAsync(
        OlvidePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await olvidePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManager.FindByEmailAsync(request.Email);
        if (usuario is null)
        {
            // No revelar si el correo existe.
            return new OlvidePasswordResponse(false, null);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        logger.LogDebug("Token de restablecimiento generado para {Email}: {Token}", usuario.Email, token);

        return environment.IsDevelopment()
            ? new OlvidePasswordResponse(true, token)
            : new OlvidePasswordResponse(true, null);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManager.FindByEmailAsync(request.Email)
            ?? throw new AppException(400, "RESET_INVALIDO", "No se pudo restablecer la contraseña.");

        var resultado = await userManager.ResetPasswordAsync(usuario, request.Token, request.PasswordNueva);
        if (!resultado.Succeeded)
        {
            throw new AppException(400, "RESET_INVALIDO", DescribirErrores(resultado.Errors));
        }

        usuario.DebeCambiarPassword = false;
        usuario.AccessFailedCount = 0;
        usuario.LockoutEnd = null;
        await userManager.UpdateAsync(usuario);
        await RevocarTokensAsync(usuario.Id, cancellationToken);
    }

    public async Task<MisPermisosResponse> MisPermisosAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId)
            ?? throw new AppException(401, "USUARIO_NO_ENCONTRADO", "Usuario no autenticado.");

        var roles = (await userManager.GetRolesAsync(usuario)).ToList();
        var permisos = await ObtenerPermisosAsync(roles, cancellationToken);

        return new MisPermisosResponse(
            usuario.Id,
            usuario.Email ?? string.Empty,
            usuario.NombreCompleto,
            usuario.DebeCambiarPassword,
            roles,
            permisos);
    }

    private async Task<LoginResponse> ConstruirLoginResponseAsync(
        AppUser usuario,
        string refresh,
        CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(usuario)).ToList();
        var permisos = await ObtenerPermisosAsync(roles, cancellationToken);
        var (accessToken, expira) = await tokenGenerator.GenerateAsync(
            new TokenRequest(
                usuario.Id,
                usuario.Email ?? string.Empty,
                usuario.NombreCompleto,
                roles,
                permisos,
                usuario.DebeCambiarPassword),
            cancellationToken);

        return new LoginResponse(
            accessToken,
            refresh,
            expira,
            usuario.DebeCambiarPassword,
            usuario.Id,
            usuario.NombreCompleto,
            roles,
            permisos);
    }

    private async Task<IReadOnlyList<string>> ObtenerPermisosAsync(
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return Array.Empty<string>();
        }

        // GetRolesAsync devuelve nombres de rol; RolesPermisos.RolId guarda el Id del rol.
        var rolesIds = await db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (rolesIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        return await (from rp in db.RolesPermisos
                      join p in db.Permisos on rp.PermisoId equals p.Id
                      where rolesIds.Contains(rp.RolId)
                      select p.Codigo)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private (string Raw, RefreshToken Entidad) CrearRefreshToken(string usuarioId, DateTime ahora)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var entidad = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = HashToken(raw),
            Expira = ahora.AddDays(options.Value.RefreshTokenDays),
            Revocado = false,
            FechaCreacion = ahora
        };
        return (raw, entidad);
    }

    private async Task RevocarTokensAsync(string usuarioId, CancellationToken cancellationToken)
    {
        await LogoutAsync(usuarioId, cancellationToken);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string DescribirErrores(IEnumerable<IdentityError> errores) =>
        string.Join(" ", errores.Select(e => e.Description));
}
