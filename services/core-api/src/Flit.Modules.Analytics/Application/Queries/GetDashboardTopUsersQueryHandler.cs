using Flit.Modules.Analytics.Application.DTOs;
using Flit.Modules.Analytics.Domain.Interfaces;

namespace Flit.Modules.Analytics.Application.Queries;

/// <summary>HU-9797 — GET /api/v1/dashboard/top-users</summary>
public sealed class GetDashboardTopUsersQueryHandler(IAnalyticsRepository analyticsRepository)
{
    public async Task<DashboardTopUsersDto> HandleAsync(
        GetDashboardTopUsersQuery query,
        CancellationToken ct = default)
    {
        var rows = await analyticsRepository.GetTopUsersAsync(
            new DashboardTopUsersFilter(query.TenantId, query.From, query.To, query.UserIds),
            limit: 5,
            ct);

        var total = rows.Sum(r => r.Count);
        var data = rows.Select(r =>
        {
            var pct = total == 0 ? 0m : Math.Round((decimal)r.Count / total * 100, 2);
            return new TopUserDto(r.UserId, r.FullName, r.Count, pct);
        }).ToList();

        return new DashboardTopUsersDto(data);
    }
}
