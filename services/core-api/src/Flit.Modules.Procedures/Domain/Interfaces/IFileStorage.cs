namespace Flit.Modules.Procedures.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> UploadAsync(
        string objectKey, Stream content, string contentType, CancellationToken ct = default);
}
