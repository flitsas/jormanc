namespace Flit.Modules.Analytics.Application.Queries;

public sealed record GetDashboardProceduresQuery(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    string? Family,
    string? Status,
    IReadOnlyList<Guid>? UserIds,
    int Page,
    int PageSize);
