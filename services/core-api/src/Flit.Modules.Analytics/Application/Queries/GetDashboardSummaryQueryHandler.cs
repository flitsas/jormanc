using Flit.Modules.Analytics.Application.DTOs;
using Flit.Modules.Analytics.Application.Helpers;
using Flit.Modules.Analytics.Domain.Interfaces;

namespace Flit.Modules.Analytics.Application.Queries;

public sealed class GetDashboardSummaryQueryHandler(IAnalyticsRepository analyticsRepository)
{
    private static readonly string[] AllFamilies = ["matricula_inicial", "traspasos", "otros"];

    public async Task<DashboardSummaryDto> HandleAsync(
        GetDashboardSummaryQuery query,
        CancellationToken ct = default)
    {
        var rows = await analyticsRepository.GetSummaryByFamilyAndStatusAsync(
            new DashboardSummaryFilter(query.TenantId, query.From, query.To),
            ct);

        var total = rows.Sum(r => r.Count);

        var byFamily = AllFamilies.Select(family =>
        {
            var familyRows = rows.Where(r => r.Family == family).ToList();
            var count = familyRows.Sum(r => r.Count);
            var pct = total == 0 ? 0m : Math.Round((decimal)count / total * 100, 2);
            var byStatus = AnalyticsStatusBuckets.Aggregate(
                familyRows.Select(r => (r.Status, r.Count)));

            return new FamilySummaryDto(family, count, pct, byStatus);
        }).ToList();

        var byStatusTotal = AnalyticsStatusBuckets.Aggregate(
            rows.Select(r => (r.Status, r.Count)));

        return new DashboardSummaryDto(
            new PeriodDto(query.From, query.To),
            new SummaryDto(total, byFamily, byStatusTotal));
    }
}
