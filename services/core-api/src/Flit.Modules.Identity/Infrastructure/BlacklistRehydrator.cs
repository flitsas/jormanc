using Flit.Modules.Identity.Domain.Interfaces;

namespace Flit.Modules.Identity.Infrastructure;

/// <summary>
/// Carga los JTIs revocados vigentes desde identity.sessions a la blacklist en memoria.
/// Scoped: necesita ISessionRepository (scoped) e ISessionBlacklist (singleton).
/// Delegado por BlacklistRehydrationService (Flit.Api) al arrancar el proceso (ADR-0013).
/// Extraído como clase independiente para permitir pruebas unitarias sin IServiceScopeFactory.
/// </summary>
public sealed class BlacklistRehydrator(
    ISessionRepository sessionRepository,
    ISessionBlacklist blacklist)
{
    /// <summary>
    /// Carga los JTIs revocados vigentes y devuelve la cantidad procesada.
    /// </summary>
    public async Task<int> RehydrateAsync(CancellationToken ct = default)
    {
        var revoked = await sessionRepository.GetActiveRevokedForBlacklistAsync(ct);

        foreach (var session in revoked)
            await blacklist.RevokeAsync(session.Jti, session.ExpiresAt, ct);

        return revoked.Count;
    }
}
