using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio de sesiones JWT para el módulo Identity.
/// </summary>
public interface ISessionRepository
{
    Task CreateAsync(Session session, CancellationToken ct = default);

    /// <summary>
    /// Retorna sesiones revocadas cuyo expires_at es mayor al momento actual.
    /// Usado por BlacklistRehydrationService al arrancar el proceso.
    /// </summary>
    Task<IReadOnlyList<Session>> GetActiveRevokedForBlacklistAsync(CancellationToken ct = default);

    /// <summary>
    /// Retorna sesiones activas (no revocadas y no expiradas) de un usuario.
    /// Usado al cambiar roles para revocar todas las sesiones vigentes (HU-9770).
    /// </summary>
    Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Busca sesión activa por JTI y usuario (logout).</summary>
    Task<Session?> GetByJtiAsync(string jti, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Marca la sesión como revocada y registra quién/cuándo la revocó.
    /// </summary>
    Task RevokeAsync(Session session, Guid revokedBy, DateTimeOffset revokedAt, CancellationToken ct = default);
}
