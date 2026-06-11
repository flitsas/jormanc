using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Queries;

/// <summary>AC3 HU-9791 — GET /procedures/{id}/consolidated (última versión).</summary>
public sealed class GetConsolidatedPackageQueryHandler(
    IDocumentRepository repository,
    IDocumentFileStorage fileStorage)
{
    public async Task<Result<(byte[] Content, string Filename), DocumentError>> HandleAsync(
        GetConsolidatedPackageQuery query, CancellationToken ct = default)
    {
        var procedure = await repository.FindProcedureAsync(query.ProcedureId, query.TenantId, ct);
        if (procedure is null)
            return Result<(byte[], string), DocumentError>.Failure(DocumentError.ProcedureNotFound);

        var package = await repository.GetLatestConsolidatedPackageAsync(
            query.ProcedureId, query.TenantId, ct);

        if (package is null)
            return Result<(byte[], string), DocumentError>.Failure(DocumentError.ConsolidatedPackageNotFound);

        var content = await fileStorage.ReadBytesAsync(package.MergedFileRef, ct);
        return Result<(byte[], string), DocumentError>.Success((content, package.DownloadFilename));
    }
}
