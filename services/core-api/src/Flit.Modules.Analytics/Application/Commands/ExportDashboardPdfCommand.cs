namespace Flit.Modules.Analytics.Application.Commands;

public sealed record ExportDashboardPdfCommand(
    Guid TenantId,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<string>? Families,
    bool IncludeCharts = true);
