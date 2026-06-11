using System.Security.Claims;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Emisor de JWT RS256 con claims: sub, jti, tid, email, name, roles[], perms[].
/// Implementa ITokenIssuer. ADR-0013: vida útil ≤15 min con claim jti para blacklist.
/// </summary>
public sealed class JwtTokenIssuer(RsaKeyProvider keyProvider, IOptions<IdentityJwtOptions> options)
    : ITokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new()
    {
        SetDefaultTimesOnTokenCreation = false
    };

    public TokenResult Issue(TokenRequest request)
    {
        var opts = options.Value;
        var jti = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(opts.ExpiresInSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new("tid", request.TenantId.ToString()),
            new("name", request.FullName)
        };

        foreach (var role in request.Roles)
            claims.Add(new Claim("roles", role));

        foreach (var perm in request.Permissions)
            claims.Add(new Claim("perms", perm));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Issuer = opts.Issuer,
            Audience = opts.Audience,
            SigningCredentials = new SigningCredentials(
                keyProvider.SigningKey,
                SecurityAlgorithms.RsaSha256)
        };

        var token = Handler.CreateToken(descriptor);
        return new TokenResult(token, jti, expiresAt, opts.ExpiresInSeconds);
    }
}
