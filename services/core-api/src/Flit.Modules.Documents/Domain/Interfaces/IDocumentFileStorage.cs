namespace Flit.Modules.Documents.Domain.Interfaces;

public interface IDocumentFileStorage
{
    Task<string> UploadAsync(
        string objectKey, Stream content, string contentType, CancellationToken ct = default);

    Task<byte[]> ReadBytesAsync(string objectKey, CancellationToken ct = default);
}
