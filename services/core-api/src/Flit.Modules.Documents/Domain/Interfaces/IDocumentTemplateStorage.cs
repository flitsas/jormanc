namespace Flit.Modules.Documents.Domain.Interfaces;

public interface IDocumentTemplateStorage
{
    Task<string> UploadAsync(
        string objectKey, Stream content, string contentType, CancellationToken ct = default);

    Task<string> ReadTextAsync(string objectKey, CancellationToken ct = default);
}
