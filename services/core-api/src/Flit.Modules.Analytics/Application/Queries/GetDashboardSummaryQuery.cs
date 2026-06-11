namespace Flit.Modules.Analytics.Application.Queries;

public sealed record GetDashboardSummaryQuery(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To);
