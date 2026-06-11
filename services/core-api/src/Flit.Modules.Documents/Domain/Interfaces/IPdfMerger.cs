namespace Flit.Modules.Documents.Domain.Interfaces;

public interface IPdfMerger
{
    byte[] Merge(IReadOnlyList<byte[]> pdfContents);
}
