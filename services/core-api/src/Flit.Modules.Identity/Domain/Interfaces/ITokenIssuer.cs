namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Emisor de JWT RS256. Inyectable para facilitar mocks en tests.
/// </summary>
public interface ITokenIssuer
{
    TokenResult Issue(TokenRequest request);
}

/// <summary>Datos requeridos para emitir un token.</summary>
public sealed record TokenRequest(
    Guid UserId,
    Guid TenantId,
    string TenantName,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>Token emitido con metadatos para crear la sesión en BD.</summary>
public sealed record TokenResult(
    string AccessToken,
    string Jti,
    DateTimeOffset ExpiresAt,
    int ExpiresIn);
