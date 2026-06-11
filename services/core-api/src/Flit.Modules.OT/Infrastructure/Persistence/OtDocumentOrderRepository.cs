using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.OT.Infrastructure.Persistence;

public sealed class OtDocumentOrderRepository(FlitDbContext db) : IOtDocumentOrderRepository
{
    public Task<OtDocumentOrder?> FindByOtAndProcedureTypeAsync(
        Guid otId, Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.OtDocumentOrders.FirstOrDefaultAsync(
            o => o.OtId == otId && o.ProcedureTypeId == procedureTypeId && o.TenantId == tenantId,
            ct);

    public async Task<IReadOnlyList<OtDocumentOrder>> ListByOtAsync(
        Guid otId, Guid tenantId, CancellationToken ct = default) =>
        await db.OtDocumentOrders
            .Where(o => o.OtId == otId && o.TenantId == tenantId)
            .OrderBy(o => o.ProcedureTypeId)
            .ToListAsync(ct);

    public async Task UpsertAsync(OtDocumentOrder order, CancellationToken ct = default)
    {
        var existing = await FindByOtAndProcedureTypeAsync(
            order.OtId, order.ProcedureTypeId, order.TenantId, ct);

        if (existing is null)
        {
            db.OtDocumentOrders.Add(order);
        }
        else
        {
            existing.OrderedDocumentTypeIds = order.OrderedDocumentTypeIds;
            existing.UpdatedAt = order.UpdatedAt;
            existing.UpdatedBy = order.UpdatedBy;
        }

        await db.SaveChangesAsync(ct);
    }
}
