namespace Flit.Modules.Analytics.Application.Queries;

public sealed record GetDashboardTopUsersQuery(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<Guid>? UserIds);
