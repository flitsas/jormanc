namespace Flit.Modules.Analytics.Application.Commands;

public sealed record ExportDashboardExcelCommand(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    string? Family,
    string? Status,
    IReadOnlyList<Guid>? UserIds,
    string Format = "xlsx");

public sealed record ExportFileResult(
    byte[] Content,
    string ContentType,
    string Filename);
