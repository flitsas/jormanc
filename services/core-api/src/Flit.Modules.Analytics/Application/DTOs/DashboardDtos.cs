namespace Flit.Modules.Analytics.Application.DTOs;

public sealed record DashboardSummaryDto(
    PeriodDto Period,
    SummaryDto Summary);

public sealed record PeriodDto(
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record SummaryDto(
    int Total,
    IReadOnlyList<FamilySummaryDto> ByFamily,
    StatusBreakdownDto ByStatus);

public sealed record FamilySummaryDto(
    string Family,
    int Count,
    decimal Pct,
    StatusBreakdownDto ByStatus);

public sealed record StatusBreakdownDto(
    int Draft,
    int Submitted,
    int Approved,
    int Rejected);

public sealed record DashboardProceduresPageDto(
    IReadOnlyList<DashboardProcedureItemDto> Data,
    int Total,
    int Page,
    int PageSize);

public sealed record DashboardProcedureItemDto(
    string Id,
    DateTimeOffset? SubmittedAt,
    string Status,
    string? Plate,
    string? OwnerName,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset UpdatedAt);

public sealed record DashboardTopUsersDto(IReadOnlyList<TopUserDto> Data);

public sealed record TopUserDto(
    Guid UserId,
    string FullName,
    int Count,
    decimal PctOfTotal);
