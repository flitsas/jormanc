using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio de invitaciones usando FlitDbContext (HU-9772).
/// </summary>
public sealed class InvitationRepository(FlitDbContext db) : IInvitationRepository
{
    public async Task CreateAsync(Invitation invitation, CancellationToken ct = default)
    {
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);
    }

    public Task<Invitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.Invitations
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task UpdateAsync(Invitation invitation, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
