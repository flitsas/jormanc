using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Domain.Services;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC1 HU-9791 — merge PDF por prelación y versionamiento de paquetes.</summary>
public sealed class ConsolidateDocumentsCommandHandler(
    IDocumentRepository repository,
    IDocumentFileStorage fileStorage,
    IPdfMerger pdfMerger,
    IClock clock)
{
    public async Task<Result<ConsolidatedPackageDto, DocumentError>> HandleAsync(
        ConsolidateDocumentsCommand command, CancellationToken ct = default)
    {
        var procedure = await repository.FindProcedureAsync(command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<ConsolidatedPackageDto, DocumentError>.Failure(DocumentError.ProcedureNotFound);

        var configs = await repository.GetAssociationsAsync(procedure.ProcedureTypeId, command.TenantId, ct);
        if (configs.Count == 0)
            return Result<ConsolidatedPackageDto, DocumentError>.Failure(DocumentError.ConsolidationNotReady);

        var documents = await repository.GetProcedureDocumentsAsync(command.ProcedureId, command.TenantId, ct);

        if (!command.Force && !DocumentCompletionChecker.CanConsolidate(configs, documents))
            return Result<ConsolidatedPackageDto, DocumentError>.Failure(DocumentError.ConsolidationNotReady);

        var orderedDocs = configs
            .Select(c => new
            {
                Config = c,
                Document = documents.FirstOrDefault(d => d.DocumentTypeId == c.DocumentTypeId)
            })
            .Where(x => x.Document is not null &&
                        x.Document.Status == "ready" &&
                        !string.IsNullOrWhiteSpace(x.Document.FileRef))
            .OrderBy(x => x.Config.OrderIndex)
            .Select(x => x.Document!)
            .ToList();

        if (orderedDocs.Count == 0)
            return Result<ConsolidatedPackageDto, DocumentError>.Failure(DocumentError.ConsolidationNotReady);

        var pdfBytes = new List<byte[]>();
        foreach (var doc in orderedDocs)
        {
            var bytes = await fileStorage.ReadBytesAsync(doc.FileRef!, ct);
            pdfBytes.Add(bytes);
        }

        var merged = pdfMerger.Merge(pdfBytes);
        var now = clock.UtcNow;
        var nextVersion = await repository.GetMaxConsolidatedVersionAsync(
            command.ProcedureId, command.TenantId, ct) + 1;

        var dateStamp = now.ToString("yyyyMMddHHmmss");
        var safeComposite = SanitizeFilenamePart(procedure.CompositeId);
        var downloadFilename = $"TRAMITE_{safeComposite}_{dateStamp}.pdf";
        var mergedFileRef = $"procedures/{command.TenantId}/{command.ProcedureId}/consolidated/v{nextVersion}/{downloadFilename}";

        await using var mergedStream = new MemoryStream(merged);
        await fileStorage.UploadAsync(mergedFileRef, mergedStream, "application/pdf", ct);

        var package = new ConsolidatedPackage
        {
            Id = Guid.NewGuid(),
            ProcedureId = command.ProcedureId,
            TenantId = command.TenantId,
            Version = nextVersion,
            MergedFileRef = mergedFileRef,
            DownloadFilename = downloadFilename,
            DocCount = orderedDocs.Count,
            CreatedAt = now,
            CreatedBy = command.UserId
        };

        await repository.CreateConsolidatedPackageAsync(package, ct);

        return Result<ConsolidatedPackageDto, DocumentError>.Success(
            new ConsolidatedPackageDto(
                package.Version,
                package.MergedFileRef,
                package.CreatedAt,
                package.DownloadFilename,
                package.DocCount));
    }

    private static string SanitizeFilenamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SIN_ID";

        var chars = value
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_')
            .ToArray();

        return new string(chars);
    }
}
