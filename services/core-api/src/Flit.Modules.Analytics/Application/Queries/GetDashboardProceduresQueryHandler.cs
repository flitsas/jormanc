using Flit.Modules.Analytics.Application.DTOs;
using Flit.Modules.Analytics.Domain.Interfaces;

namespace Flit.Modules.Analytics.Application.Queries;

public sealed class GetDashboardProceduresQueryHandler(IAnalyticsRepository analyticsRepository)
{
    public async Task<DashboardProceduresPageDto> HandleAsync(
        GetDashboardProceduresQuery query,
        CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 50);

        var filter = new DashboardProceduresFilter(
            TenantId: query.TenantId,
            From: query.From,
            To: query.To,
            Family: query.Family,
            Status: query.Status,
            UserIds: query.UserIds,
            Page: page,
            PageSize: pageSize);

        var (items, total) = await analyticsRepository.GetProceduresDetailAsync(filter, ct);

        return new DashboardProceduresPageDto(
            Data: items.Select(row => new DashboardProcedureItemDto(
                Id: row.CompositeId,
                SubmittedAt: row.SubmittedAt,
                Status: row.Status,
                Plate: row.Plate,
                OwnerName: row.OwnerName,
                ApprovedAt: row.ApprovedAt,
                UpdatedAt: row.UpdatedAt)).ToList(),
            Total: total,
            Page: page,
            PageSize: pageSize);
    }
}
