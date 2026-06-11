namespace Flit.Modules.Analytics.Domain.Interfaces;

public sealed record DashboardSummaryFilter(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record DashboardProceduresFilter(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    string? Family,
    string? Status,
    IReadOnlyList<Guid>? UserIds,
    int Page,
    int PageSize);

public sealed record DashboardExportFilter(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    string? Family,
    string? Status,
    IReadOnlyList<Guid>? UserIds);

public sealed record ProcedureSummaryRow(string Family, string Status, int Count);

public sealed record ProcedureDetailRow(
    string CompositeId,
    DateTimeOffset? SubmittedAt,
    string Status,
    string? Plate,
    string? OwnerName,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset UpdatedAt);

public sealed record DashboardTopUsersFilter(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<Guid>? UserIds);

public sealed record TopUserRow(
    Guid UserId,
    string FullName,
    int Count);

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<ProcedureSummaryRow>> GetSummaryByFamilyAndStatusAsync(
        DashboardSummaryFilter filter,
        CancellationToken ct = default);

    Task<(IReadOnlyList<ProcedureDetailRow> Items, int Total)> GetProceduresDetailAsync(
        DashboardProceduresFilter filter,
        CancellationToken ct = default);

    IAsyncEnumerable<IReadOnlyList<ProcedureDetailRow>> StreamProceduresBatchesAsync(
        DashboardExportFilter filter,
        int batchSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<TopUserRow>> GetTopUsersAsync(
        DashboardTopUsersFilter filter,
        int limit = 5,
        CancellationToken ct = default);
}
