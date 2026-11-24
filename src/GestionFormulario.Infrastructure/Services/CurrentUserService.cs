using System.Security.Claims;
using GestionFormulario.Application.Common;
using Microsoft.AspNetCore.Http;

namespace GestionFormulario.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return Array.Empty<string>();
            }

            return user.FindAll("role")
                .Select(x => x.Value)
                .Concat(user.FindAll(ClaimTypes.Role).Select(x => x.Value))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
