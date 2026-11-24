using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionFormulario.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using GestionFormulario.Infrastructure.Configuration;

namespace GestionFormulario.Infrastructure.Services;

public sealed class JwtTokenGenerator(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IJwtTokenGenerator
{
    public Task<(string Token, DateTime Expira)> GenerateAsync(
        TokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var jwt = options.Value;
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new("sub", request.UsuarioId),
            new("email", request.Email),
            new("nombre", request.NombreCompleto),
            new("debeCambiarPassword", request.DebeCambiarPassword.ToString().ToLowerInvariant())
        };
        claims.AddRange(request.Roles.Select(rol => new Claim("role", rol)));
        claims.AddRange(request.Permisos.Select(permiso => new Claim("permiso", permiso)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: ahora,
            expires: expira,
            signingCredentials: credenciales);

        return Task.FromResult((new JwtSecurityTokenHandler().WriteToken(token), expira));
    }
}
