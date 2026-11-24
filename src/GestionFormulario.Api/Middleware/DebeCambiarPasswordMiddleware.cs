namespace GestionFormulario.Api.Middleware;

/// <summary>
/// Obliga a cambiar la contraseña temporal del primer login antes de usar el resto de la API.
/// </summary>
public sealed class DebeCambiarPasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] RutasPermitidas = ["/api/auth/", "/health", "/swagger"];

    public async Task InvokeAsync(HttpContext context)
    {
        var debeCambiar = context.User.FindFirst("debeCambiarPassword")?.Value;
        var ruta = context.Request.Path.Value ?? string.Empty;

        if (context.User.Identity?.IsAuthenticated == true &&
            string.Equals(debeCambiar, "true", StringComparison.OrdinalIgnoreCase) &&
            !RutasPermitidas.Any(x => ruta.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
        {
            context.Items["ProblemaCodigo"] = "CAMBIO_PASSWORD_REQUERIDO";
            context.Items["ProblemaTitulo"] = "Debes cambiar tu contraseña antes de continuar.";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
