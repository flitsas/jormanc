using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

/// <summary>Almacenamiento local DEV — simula MinIO con rutas procedures/...</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flit-storage");

    public async Task<string> UploadAsync(
        string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var basePath = Path.Combine(_root, objectKey.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(basePath)!;
        Directory.CreateDirectory(dir);

        await using var file = File.Create(basePath);
        await content.CopyToAsync(file, ct);

        return objectKey;
    }
}
