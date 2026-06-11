using Flit.Modules.Documents.Domain.Interfaces;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Flit.Modules.Documents.Infrastructure.Pdf;

/// <summary>
/// Renderiza HTML resuelto a PDF via QuestPDF.
/// Usa HtmlAgilityPack para extraer texto estructurado (compatible net10; QuestPDF.HTML aún no).
/// </summary>
public sealed class QuestPdfDocumentRenderer : IDocumentPdfRenderer
{
    static QuestPdfDocumentRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] RenderHtmlToPdf(string resolvedHtml)
    {
        var plainText = ExtractVisibleText(resolvedHtml);

        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));
                page.Content().Column(column =>
                {
                    foreach (var line in plainText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        column.Item().PaddingBottom(4).Text(line);
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    internal static string ExtractVisibleText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return HtmlEntity.DeEntitize(doc.DocumentNode.InnerText).Trim();
    }
}
