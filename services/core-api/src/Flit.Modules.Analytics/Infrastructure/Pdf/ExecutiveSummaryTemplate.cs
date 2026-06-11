using Flit.Modules.Analytics.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Flit.Modules.Analytics.Infrastructure.Pdf;

/// <summary>
/// Plantilla QuestPDF — Resumen Ejecutivo del dashboard (HU-9795 AC2).
/// </summary>
public static class ExecutiveSummaryTemplate
{
    static ExecutiveSummaryTemplate()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(DashboardSummaryDto summary, bool includeCharts)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("Resumen Ejecutivo — Dashboard de Trámites")
                        .FontSize(18).Bold();
                    column.Item().PaddingTop(4).Text(
                        $"Período: {summary.Period.From:yyyy-MM-dd} — {summary.Period.To:yyyy-MM-dd}");
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Item().Text("KPIs por familia").FontSize(14).Bold();
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Familia").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Total").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("%").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Borrador").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Radicado").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Aprobado").Bold();
                        });

                        foreach (var family in summary.Summary.ByFamily)
                        {
                            table.Cell().Padding(4).Text(FormatFamilyLabel(family.Family));
                            table.Cell().Padding(4).Text(family.Count.ToString());
                            table.Cell().Padding(4).Text($"{family.Pct}%");
                            table.Cell().Padding(4).Text(family.ByStatus.Draft.ToString());
                            table.Cell().Padding(4).Text(family.ByStatus.Submitted.ToString());
                            table.Cell().Padding(4).Text(family.ByStatus.Approved.ToString());
                        }

                        table.Cell().Padding(4).Text("TOTAL").Bold();
                        table.Cell().Padding(4).Text(summary.Summary.Total.ToString()).Bold();
                        table.Cell().Padding(4).Text("100%").Bold();
                        table.Cell().Padding(4).Text(summary.Summary.ByStatus.Draft.ToString()).Bold();
                        table.Cell().Padding(4).Text(summary.Summary.ByStatus.Submitted.ToString()).Bold();
                        table.Cell().Padding(4).Text(summary.Summary.ByStatus.Approved.ToString()).Bold();
                    });

                    if (includeCharts && summary.Summary.Total > 0)
                    {
                        column.Item().PaddingTop(24).Text("Distribución por familia").FontSize(14).Bold();
                        column.Item().PaddingTop(8).Element(c => DrawFamilyChart(c, summary.Summary.ByFamily));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado: ");
                    text.Span(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"));
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    private static void DrawFamilyChart(IContainer container, IReadOnlyList<FamilySummaryDto> families)
    {
        var segments = families.Where(f => f.Count > 0).ToList();
        if (segments.Count == 0)
            return;

        var colors = new[] { Colors.Blue.Medium, Colors.Green.Medium, Colors.Orange.Medium };
        var total = segments.Sum(f => f.Count);

        container.Column(column =>
        {
            foreach (var (segment, index) in segments.Select((s, i) => (s, i)))
            {
                var pct = (decimal)segment.Count / total * 100m;
                var color = colors[index % colors.Length];

                column.Item().PaddingBottom(6).Row(row =>
                {
                    row.ConstantItem(120).Text(FormatFamilyLabel(segment.Family)).FontSize(10);
                    row.RelativeItem().Height(18).Background(color);
                    row.ConstantItem(80).AlignRight().Text($"{segment.Count} ({pct:0.#}%)").FontSize(10);
                });
            }
        });
    }

    private static string FormatFamilyLabel(string family) => family switch
    {
        "matricula_inicial" => "Matrícula inicial",
        "traspasos" => "Traspasos",
        "otros" => "Otros",
        _ => family
    };

    public static string BuildFilename() =>
        $"resumen-ejecutivo-{DateTimeOffset.UtcNow:yyyyMMdd}.pdf";
}
