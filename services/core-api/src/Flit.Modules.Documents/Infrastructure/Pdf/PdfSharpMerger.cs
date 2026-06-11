using Flit.Modules.Documents.Domain.Interfaces;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace Flit.Modules.Documents.Infrastructure.Pdf;

/// <summary>Merge de N PDFs en un único stream consolidado (ADR-0012).</summary>
public sealed class PdfSharpMerger : IPdfMerger
{
    public byte[] Merge(IReadOnlyList<byte[]> pdfContents)
    {
        if (pdfContents.Count == 0)
            throw new InvalidOperationException("Se requiere al menos un PDF para consolidar.");

        using var output = new PdfDocument();

        foreach (var content in pdfContents)
        {
            using var inputStream = new MemoryStream(content);
            using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            for (var i = 0; i < inputDoc.PageCount; i++)
                output.AddPage(inputDoc.Pages[i]);
        }

        using var outStream = new MemoryStream();
        output.Save(outStream, false);
        return outStream.ToArray();
    }
}
