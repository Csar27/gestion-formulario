namespace GestionFormulario.Application.Common;

/// <summary>Abstracción para resolver el usuario autenticado desde capas superiores.</summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    IReadOnlyList<string> Roles { get; }
    bool EsAdmin => Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
    bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
}
