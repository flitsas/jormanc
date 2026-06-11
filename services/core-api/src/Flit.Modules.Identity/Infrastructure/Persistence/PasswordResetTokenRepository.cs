using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio de tokens de reset de contraseña usando FlitDbContext (HU-9772).
/// </summary>
public sealed class PasswordResetTokenRepository(FlitDbContext db) : IPasswordResetTokenRepository
{
    public async Task CreateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.PasswordResetTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
