using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.OT.Infrastructure.Persistence;

public sealed class OtDocumentLabelRepository(FlitDbContext db) : IOtDocumentLabelRepository
{
    public Task<bool> SlugExistsAsync(
        Guid otId, string slug, Guid tenantId, CancellationToken ct = default) =>
        db.OtDocumentLabels.AnyAsync(
            l => l.OtId == otId && l.TenantId == tenantId && l.Slug == slug,
            ct);

    public Task<OtDocumentLabel?> FindByIdAsync(
        Guid id, Guid otId, Guid tenantId, CancellationToken ct = default) =>
        db.OtDocumentLabels.FirstOrDefaultAsync(
            l => l.Id == id && l.OtId == otId && l.TenantId == tenantId,
            ct);

    public async Task<IReadOnlyList<OtDocumentLabel>> ListByOtAsync(
        Guid otId, Guid tenantId, CancellationToken ct = default) =>
        await db.OtDocumentLabels
            .Where(l => l.OtId == otId && l.TenantId == tenantId)
            .OrderBy(l => l.DisplayName)
            .ToListAsync(ct);

    public async Task CreateAsync(OtDocumentLabel label, CancellationToken ct = default)
    {
        db.OtDocumentLabels.Add(label);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OtDocumentLabel label, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task DeleteAsync(OtDocumentLabel label, CancellationToken ct = default)
    {
        db.OtDocumentLabels.Remove(label);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountAttachmentImpactAsync(
        Guid otId, string labelSlug, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureAttachments.CountAsync(
            a => a.TenantId == tenantId &&
                 a.LabelSlug == labelSlug &&
                 a.Procedure.OtId == otId,
            ct);
}
