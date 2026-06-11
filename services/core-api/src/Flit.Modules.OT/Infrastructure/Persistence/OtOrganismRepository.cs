using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.OT.Infrastructure.Persistence;

public sealed class OtOrganismRepository(FlitDbContext db) : IOtOrganismRepository
{
    public Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default) =>
        db.OtOrganisms.AnyAsync(o => o.TenantId == tenantId && o.Slug == slug, ct);

    public async Task<IReadOnlyList<OtOrganism>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.OtOrganisms
            .Where(o => o.TenantId == tenantId)
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

    public Task<OtOrganism?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.OtOrganisms.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct);

    public Task<OtOrganism?> FindBySlugAsync(string slug, CancellationToken ct = default) =>
        db.OtOrganisms.FirstOrDefaultAsync(
            o => o.Slug == slug.Trim().ToLowerInvariant() && o.DeletedAt == null, ct);

    public async Task CreateAsync(OtOrganism organism, CancellationToken ct = default)
    {
        db.OtOrganisms.Add(organism);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OtOrganism organism, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task SoftDeleteAsync(
        OtOrganism organism, Guid deletedBy, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        organism.DeletedAt = deletedAt;
        organism.DeletedBy = deletedBy;
        organism.UpdatedAt = deletedAt;
        organism.UpdatedBy = deletedBy;
        await db.SaveChangesAsync(ct);
    }
}
