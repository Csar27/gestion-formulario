using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GestionFormulario.Api.Authorization;

/// <summary>
/// Crea políticas "Permiso:Modulo.Accion" en tiempo de ejecución para no tener que
/// registrar 30 políticas estáticas en Program.cs.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefijo = "Permiso:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) =>
        _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase))
        {
            var codigo = policyName[Prefijo.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("permiso", codigo)
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
