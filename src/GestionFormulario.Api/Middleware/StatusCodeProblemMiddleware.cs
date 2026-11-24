using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Middleware;

/// <summary>Convierte los 401/403 vacíos de ASP.NET Core en ProblemDetails con código estable.</summary>
public sealed class StatusCodeProblemMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted || context.Response.StatusCode is not (401 or 403))
        {
            return;
        }

        var status = context.Response.StatusCode;
        var es401 = status == 401;
        var codigo = context.Items.TryGetValue("ProblemaCodigo", out var c) ? c?.ToString() : null;
        var titulo = context.Items.TryGetValue("ProblemaTitulo", out var t) ? t?.ToString() : null;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = titulo ?? (es401 ? "Autenticación requerida" : "Acceso denegado"),
            Detail = titulo ?? (es401
                ? "Debes iniciar sesión para acceder a este recurso."
                : "No tienes permiso para acceder a este recurso.")
        };
        problem.Extensions["code"] = codigo ?? (es401 ? "AUTENTICACION_REQUERIDA" : "ACCESO_DENEGADO");

        await context.Response.WriteAsJsonAsync(problem);
    }
}
