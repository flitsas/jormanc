using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Flit.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementación de ISessionBlacklist con IMemoryCache. ADR-0013.
/// TTL de cada entrada = tiempo de expiración del JWT (máx 15 min).
/// Volátil: se pierde en reinicios del proceso → mitigado por BlacklistRehydrationService.
/// </summary>
public sealed class InMemorySessionBlacklist(IMemoryCache cache) : ISessionBlacklist
{
    private const string KeyPrefix = "jti:revoked:";

    public Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero)
            cache.Set($"{KeyPrefix}{jti}", true, ttl);
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
        => Task.FromResult(cache.TryGetValue($"{KeyPrefix}{jti}", out _));
}
