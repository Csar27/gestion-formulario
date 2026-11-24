using Microsoft.AspNetCore.Authorization;

namespace GestionFormulario.Api.Authorization;

/// <summary>
/// Restringe un endpoint al código de permiso indicado, por ejemplo [HasPermission("Catalogos.Editar")].
/// La política se crea dinámicamente en PermissionPolicyProvider.
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string codigo) => Policy = $"Permiso:{codigo}";
}
