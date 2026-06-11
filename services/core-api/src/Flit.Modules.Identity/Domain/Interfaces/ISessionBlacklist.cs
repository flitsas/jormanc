namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Blacklist de JTIs revocados. Implementación con IMemoryCache (ADR-0013).
/// La interfaz permite migrar a Redis sin cambiar los handlers.
/// </summary>
public interface ISessionBlacklist
{
    /// <summary>
    /// Marca un JTI como revocado con TTL igual a su tiempo de expiración.
    /// </summary>
    Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Retorna true si el JTI está en la blacklist (token revocado).
    /// </summary>
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
}
