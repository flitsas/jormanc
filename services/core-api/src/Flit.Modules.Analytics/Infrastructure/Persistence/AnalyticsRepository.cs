using System.Runtime.CompilerServices;
using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Analytics.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Analytics.Infrastructure.Persistence;

/// <summary>
/// Consultas analíticas sobre procedures + procedure_types (MVP EF LINQ).
/// </summary>
public sealed class AnalyticsRepository(FlitDbContext db) : IAnalyticsRepository
{
    public async Task<IReadOnlyList<ProcedureSummaryRow>> GetSummaryByFamilyAndStatusAsync(
        DashboardSummaryFilter filter,
        CancellationToken ct = default)
    {
        var rows = await (
            from p in db.Procedures.AsNoTracking()
            join pt in db.ProcedureTypes.AsNoTracking() on p.ProcedureTypeId equals pt.Id
            where p.TenantId == filter.TenantId
                  && p.DeletedAt == null
                  && (p.SubmittedAt ?? p.CreatedAt) >= filter.From
                  && (p.SubmittedAt ?? p.CreatedAt) <= filter.To
            group p by new { pt.Family, p.Status } into g
            select new ProcedureSummaryRow(g.Key.Family, g.Key.Status, g.Count())
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<(IReadOnlyList<ProcedureDetailRow> Items, int Total)> GetProceduresDetailAsync(
        DashboardProceduresFilter filter,
        CancellationToken ct = default)
    {
        var query = BuildProceduresQuery(filter.TenantId, filter.From, filter.To, filter.Family, filter.Status, filter.UserIds);

        var total = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 50);

        var pageProcedures = await query
            .OrderByDescending(x => x.Procedure.SubmittedAt ?? x.Procedure.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Procedure)
            .ToListAsync(ct);

        var items = await MapProcedureRowsAsync(pageProcedures, ct);
        return (items, total);
    }

    public async IAsyncEnumerable<IReadOnlyList<ProcedureDetailRow>> StreamProceduresBatchesAsync(
        DashboardExportFilter filter,
        int batchSize,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var effectiveBatchSize = batchSize < 1 ? 500 : Math.Min(batchSize, 500);
        var query = BuildProceduresQuery(filter.TenantId, filter.From, filter.To, filter.Family, filter.Status, filter.UserIds);

        var page = 1;
        while (true)
        {
            var pageProcedures = await query
                .OrderByDescending(x => x.Procedure.SubmittedAt ?? x.Procedure.CreatedAt)
                .Skip((page - 1) * effectiveBatchSize)
                .Take(effectiveBatchSize)
                .Select(x => x.Procedure)
                .ToListAsync(ct);

            if (pageProcedures.Count == 0)
                yield break;

            yield return await MapProcedureRowsAsync(pageProcedures, ct);

            if (pageProcedures.Count < effectiveBatchSize)
                yield break;

            page++;
        }
    }

    private IQueryable<ProcedureJoinRow> BuildProceduresQuery(
        Guid tenantId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        string? family,
        string? status,
        IReadOnlyList<Guid>? userIds)
    {
        var query =
            from p in db.Procedures.AsNoTracking()
            join pt in db.ProcedureTypes.AsNoTracking() on p.ProcedureTypeId equals pt.Id
            where p.TenantId == tenantId
                  && p.DeletedAt == null
                  && (p.SubmittedAt ?? p.CreatedAt) >= fromDate
                  && (p.SubmittedAt ?? p.CreatedAt) <= toDate
            select new ProcedureJoinRow(p, pt.Family);

        if (!string.IsNullOrWhiteSpace(family))
            query = query.Where(x => x.TypeFamily == family);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Procedure.Status == status);

        if (userIds is { Count: > 0 })
        {
            query = query.Where(x =>
                (x.Procedure.AssignedUserId != null && userIds.Contains(x.Procedure.AssignedUserId.Value))
                || userIds.Contains(x.Procedure.CreatedBy));
        }

        return query;
    }

    private async Task<IReadOnlyList<ProcedureDetailRow>> MapProcedureRowsAsync(
        IReadOnlyList<Procedure> pageProcedures,
        CancellationToken ct)
    {
        if (pageProcedures.Count == 0)
            return Array.Empty<ProcedureDetailRow>();

        var procedureIds = pageProcedures.Select(p => p.Id).ToList();

        var vehicleQueries = await db.VehicleQueries.AsNoTracking()
            .Where(v => procedureIds.Contains(v.ProcedureId) && v.QueryKey == "placa")
            .ToListAsync(ct);

        var actors = await db.ProcedureActors.AsNoTracking()
            .Where(a => procedureIds.Contains(a.ProcedureId) && a.FullName != null)
            .ToListAsync(ct);

        return pageProcedures.Select(p =>
        {
            var plate = vehicleQueries
                .Where(v => v.ProcedureId == p.Id)
                .OrderBy(v => v.QueriedAt)
                .Select(v => v.QueryValue)
                .FirstOrDefault();

            var ownerName = actors
                .Where(a => a.ProcedureId == p.Id)
                .OrderBy(a => a.CreatedAt)
                .Select(a => a.FullName)
                .FirstOrDefault();

            return new ProcedureDetailRow(
                CompositeId: p.CompositeId,
                SubmittedAt: p.SubmittedAt,
                Status: p.Status,
                Plate: plate,
                OwnerName: ownerName,
                ApprovedAt: p.ApprovedAt,
                UpdatedAt: p.UpdatedAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<TopUserRow>> GetTopUsersAsync(
        DashboardTopUsersFilter filter,
        int limit = 5,
        CancellationToken ct = default)
    {
        var effectiveLimit = limit < 1 ? 5 : Math.Min(limit, 20);

        var query =
            from p in db.Procedures.AsNoTracking()
            where p.TenantId == filter.TenantId
                  && p.DeletedAt == null
                  && p.SubmittedAt != null
                  && p.SubmittedAt >= filter.From
                  && p.SubmittedAt <= filter.To
            select p;

        if (filter.UserIds is { Count: > 0 })
        {
            query = query.Where(p =>
                filter.UserIds.Contains(p.CreatedBy)
                || (p.AssignedUserId != null && filter.UserIds.Contains(p.AssignedUserId.Value)));
        }

        var grouped = await query
            .GroupBy(p => p.CreatedBy)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(effectiveLimit)
            .ToListAsync(ct);

        if (grouped.Count == 0)
            return Array.Empty<TopUserRow>();

        var userIds = grouped.Select(g => g.UserId).ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        var nameById = users.ToDictionary(u => u.Id, u => u.FullName);

        return grouped
            .Select(g => new TopUserRow(
                g.UserId,
                nameById.GetValueOrDefault(g.UserId, "Usuario desconocido"),
                g.Count))
            .ToList();
    }

    private sealed record ProcedureJoinRow(Procedure Procedure, string TypeFamily);
}
