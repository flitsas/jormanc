using System.Text;
using Flit.Modules.Documents.Domain.Interfaces;

namespace Flit.Modules.Documents.Infrastructure.Storage;

/// <summary>Almacenamiento local DEV — plantillas HTML versionadas (templates/...).</summary>
public sealed class LocalDocumentTemplateStorage : IDocumentTemplateStorage
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

    public async Task<string> ReadTextAsync(string objectKey, CancellationToken ct = default)
    {
        var basePath = ResolvePath(objectKey);
        if (!File.Exists(basePath))
            throw new FileNotFoundException($"Template not found: {objectKey}", basePath);

        return await File.ReadAllTextAsync(basePath, Encoding.UTF8, ct);
    }

    private string ResolvePath(string objectKey) =>
        Path.Combine(_root, objectKey.Replace('/', Path.DirectorySeparatorChar));
}
