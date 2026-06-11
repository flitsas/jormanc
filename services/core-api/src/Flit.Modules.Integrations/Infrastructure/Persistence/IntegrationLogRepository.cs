using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Integrations.Infrastructure.Persistence;

/// <summary>
/// Implementación EF Core del repositorio de integration_logs.
/// Los logs son inmutables — solo INSERT y lectura.
/// AC3 HU-9775: cada llamada al conector RUNT genera un registro por tenant.
/// </summary>
public sealed class IntegrationLogRepository(FlitDbContext db) : IIntegrationLogRepository
{
    public async Task AddAsync(IntegrationLog log, CancellationToken ct = default)
    {
        db.IntegrationLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<IntegrationLog> Items, int Total)> ListAsync(
        IntegrationLogFilter filter, CancellationToken ct = default)
    {
        var query = db.IntegrationLogs
            .Where(l => l.TenantId == filter.TenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.ConnectorType))
            query = query.Where(l => l.ConnectorType == filter.ConnectorType);

        if (!string.IsNullOrWhiteSpace(filter.Provider))
            query = query.Where(l => l.Provider == filter.Provider);

        if (filter.From.HasValue)
            query = query.Where(l => l.LoggedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(l => l.LoggedAt <= filter.To.Value);

        var total = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

        var items = await query
            .OrderByDescending(l => l.LoggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
