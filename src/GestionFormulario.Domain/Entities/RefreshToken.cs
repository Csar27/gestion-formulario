namespace GestionFormulario.Domain.Entities;

/// <summary>
/// Se almacena el hash del token, nunca el token en claro.
/// ReemplazadoPor permite detectar reuso de un token rotado y revocar la familia.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public bool Revocado { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public Guid? ReemplazadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
}
