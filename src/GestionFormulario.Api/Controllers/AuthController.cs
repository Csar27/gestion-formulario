using GestionFormulario.Application.Common;
using GestionFormulario.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
        => Ok(await authService.RefreshAsync(request, cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(UsuarioActual(), cancellationToken);
        return Ok(new OperacionResponse(true, "Sesión cerrada."));
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword(
        [FromBody] CambiarPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.CambiarPasswordAsync(UsuarioActual(), request, cancellationToken);
        return Ok(new OperacionResponse(true, "Contraseña actualizada."));
    }

    [HttpPost("olvide-password")]
    [AllowAnonymous]
    public async Task<IActionResult> OlvidePassword(
        [FromBody] OlvidePasswordRequest request,
        CancellationToken cancellationToken)
        => Ok(await authService.OlvidePasswordAsync(request, cancellationToken));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new OperacionResponse(true, "Contraseña restablecida."));
    }

    [HttpGet("mis-permisos")]
    [Authorize]
    public async Task<IActionResult> MisPermisos(CancellationToken cancellationToken)
        => Ok(await authService.MisPermisosAsync(UsuarioActual(), cancellationToken));

    private string UsuarioActual() =>
        currentUser.UserId ?? throw new AppException(401, "AUTENTICACION_REQUERIDA", "Usuario no autenticado.");
}
