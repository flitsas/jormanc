using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Procedures.Infrastructure.Persistence;

public sealed class ProcedureRepository(FlitDbContext db) : IProcedureRepository
{
    public async Task CreateAsync(Procedure procedure, CancellationToken ct = default)
    {
        db.Procedures.Add(procedure);
        await db.SaveChangesAsync(ct);
    }

    public Task<Procedure?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.Procedures.FirstOrDefaultAsync(
            p => p.Id == id && p.TenantId == tenantId, ct);

    public Task<Procedure?> FindByCompositeIdAsync(string compositeId, Guid tenantId, CancellationToken ct = default) =>
        db.Procedures.FirstOrDefaultAsync(
            p => p.CompositeId == compositeId && p.TenantId == tenantId, ct);

    public Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Procedures.CountAsync(p => p.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<Procedure> Items, int Total)> ListAsync(
        ProcedureListFilter filter, CancellationToken ct = default)
    {
        var query = db.Procedures.AsNoTracking()
            .Where(p => p.TenantId == filter.TenantId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.Status == filter.Status);

        if (filter.DateFrom.HasValue)
            query = query.Where(p => p.CreatedAt >= filter.DateFrom.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddVehicleQueryAsync(VehicleQuery vehicleQuery, CancellationToken ct = default)
    {
        db.VehicleQueries.Add(vehicleQuery);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddActorsAsync(IReadOnlyList<ProcedureActor> actors, CancellationToken ct = default)
    {
        db.ProcedureActors.AddRange(actors);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProcedureActor>> GetActorsAsync(
        Guid procedureId, Guid tenantId, Guid? actorDefinitionId = null, CancellationToken ct = default)
    {
        var query = db.ProcedureActors.AsNoTracking()
            .Where(a => a.ProcedureId == procedureId && a.TenantId == tenantId);

        if (actorDefinitionId.HasValue)
            query = query.Where(a => a.ActorDefinitionId == actorDefinitionId.Value);

        return await query.ToListAsync(ct);
    }

    public async Task UpdateAsync(Procedure procedure, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task AddAttachmentAsync(ProcedureAttachment attachment, CancellationToken ct = default)
    {
        db.ProcedureAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProcedureAttachment>> GetAttachmentsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default)
    {
        var items = await db.ProcedureAttachments.AsNoTracking()
            .Where(a => a.ProcedureId == procedureId && a.TenantId == tenantId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(ct);
        return items;
    }
}
