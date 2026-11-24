namespace GestionFormulario.Application.Common;

public sealed record TokenRequest(
    string UsuarioId,
    string Email,
    string NombreCompleto,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permisos,
    bool DebeCambiarPassword);

public interface IJwtTokenGenerator
{
    Task<(string Token, DateTime Expira)> GenerateAsync(TokenRequest request, CancellationToken cancellationToken = default);
}
