using FluentValidation;
using GestionFormulario.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace GestionFormulario.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await EscribirAsync(
                context,
                400,
                "VALIDACION",
                "Uno o más campos no son válidos.",
                ex.Errors.Select(e => e.ErrorMessage).ToList());
        }
        catch (AppException ex)
        {
            await EscribirAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado al procesar {Method} {Path}", context.Request.Method, context.Request.Path);
            await EscribirAsync(context, 500, "ERROR_INTERNO", "Ocurrió un error inesperado.");
        }
    }

    private static async Task EscribirAsync(
        HttpContext context,
        int status,
        string codigo,
        string titulo,
        IReadOnlyList<string>? errores = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = titulo
        };
        problem.Extensions["code"] = codigo;
        if (errores is { Count: > 0 })
        {
            problem.Extensions["errors"] = errores;
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
