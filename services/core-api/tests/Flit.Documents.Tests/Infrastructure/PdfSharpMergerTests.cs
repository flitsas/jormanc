using FluentAssertions;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Xunit;

namespace Flit.Documents.Tests.Infrastructure;

public class PdfSharpMergerTests
{
    [Fact]
    public void AC1_MergeDosPdfsProducePdfValido()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var merger = new PdfSharpMerger();

        var pdf1 = renderer.RenderHtmlToPdf("<html><body><p>Doc 1</p></body></html>");
        var pdf2 = renderer.RenderHtmlToPdf("<html><body><p>Doc 2</p></body></html>");

        var merged = merger.Merge([pdf1, pdf2]);

        merged.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(merged, 0, 4).Should().Be("%PDF");
    }
}
