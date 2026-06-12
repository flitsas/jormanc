using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio de sesiones JWT usando FlitDbContext.
/// </summary>
public sealed class SessionRepository(FlitDbContext db) : ISessionRepository
{
    public async Task CreateAsync(Session session, CancellationToken ct = default)
    {
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Session>> GetActiveRevokedForBlacklistAsync(
        CancellationToken ct = default)
    {
        var list = await db.Sessions
            .Where(s => s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        return list;
    }

    public async Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var list = await db.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        return list;
    }

    public async Task<Session?> GetByJtiAsync(
        string jti, Guid userId, CancellationToken ct = default)
    {
        return await db.Sessions
            .FirstOrDefaultAsync(
                s => s.Jti == jti && s.UserId == userId && !s.IsRevoked,
                ct);
    }

    public async Task RevokeAsync(
        Session session, Guid revokedBy, DateTimeOffset revokedAt, CancellationToken ct = default)
    {
        session.IsRevoked = true;
        session.RevokedAt = revokedAt;
        session.RevokedBy = revokedBy;
        await db.SaveChangesAsync(ct);
    }
}
