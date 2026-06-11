using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Infrastructure.Pdf;

namespace Flit.Modules.Analytics.Application.Commands;

/// <summary>HU-9795 AC2 — Resumen Ejecutivo PDF con QuestPDF.</summary>
public sealed class ExportDashboardPdfCommandHandler(
    GetDashboardSummaryQueryHandler summaryQueryHandler)
{
    public const string PdfContentType = "application/pdf";

    public async Task<ExportFileResult> HandleAsync(
        ExportDashboardPdfCommand command,
        CancellationToken ct = default)
    {
        var summary = await summaryQueryHandler.HandleAsync(
            new GetDashboardSummaryQuery(command.TenantId, command.From, command.To),
            ct);

        if (command.Families is { Count: > 0 })
        {
            var allowed = command.Families.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var filteredFamilies = summary.Summary.ByFamily
                .Where(f => allowed.Contains(f.Family))
                .ToList();

            var filteredTotal = filteredFamilies.Sum(f => f.Count);
            summary = summary with
            {
                Summary = summary.Summary with
                {
                    Total = filteredTotal,
                    ByFamily = filteredFamilies
                }
            };
        }

        var pdf = ExecutiveSummaryTemplate.Generate(summary, command.IncludeCharts);

        return new ExportFileResult(
            pdf,
            PdfContentType,
            ExecutiveSummaryTemplate.BuildFilename());
    }
}
