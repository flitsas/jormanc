namespace Flit.Modules.Documents.Domain.Interfaces;

public interface IDocumentPdfRenderer
{
    byte[] RenderHtmlToPdf(string resolvedHtml);
}
