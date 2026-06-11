using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.OT.Infrastructure.Persistence;

public sealed class OtIntegrationLogRepository(FlitDbContext db) : IOtIntegrationLogRepository
{
  public async Task AddAsync(OtIntegrationLog log, CancellationToken ct = default)
  {
    db.OtIntegrationLogs.Add(log);
    await db.SaveChangesAsync(ct);
  }

  public async Task<(IReadOnlyList<OtIntegrationLog> Items, int Total)> ListAsync(
    OtIntegrationLogFilter filter,
    CancellationToken ct = default)
  {
    var query = db.OtIntegrationLogs
      .AsNoTracking()
      .Where(l => l.TenantId == filter.TenantId && l.OtId == filter.OtId);

    if (!string.IsNullOrWhiteSpace(filter.EventType))
      query = query.Where(l => l.EventType == filter.EventType);

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
