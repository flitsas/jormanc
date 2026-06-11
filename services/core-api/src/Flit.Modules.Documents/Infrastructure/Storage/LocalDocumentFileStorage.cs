using Flit.Modules.Documents.Domain.Interfaces;

namespace Flit.Modules.Documents.Infrastructure.Storage;

/// <summary>Almacenamiento local DEV — simula MinIO para PDFs y paquetes consolidados.</summary>
public sealed class LocalDocumentFileStorage : IDocumentFileStorage
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flit-storage");

    public async Task<string> UploadAsync(
        string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var basePath = ResolvePath(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);

        await using var file = File.Create(basePath);
        await content.CopyToAsync(file, ct);

        return objectKey;
    }

    public async Task<byte[]> ReadBytesAsync(string objectKey, CancellationToken ct = default)
    {
        var basePath = ResolvePath(objectKey);
        if (!File.Exists(basePath))
            throw new FileNotFoundException($"Object not found: {objectKey}", basePath);

        return await File.ReadAllBytesAsync(basePath, ct);
    }

    private string ResolvePath(string objectKey) =>
        Path.Combine(_root, objectKey.Replace('/', Path.DirectorySeparatorChar));
}
