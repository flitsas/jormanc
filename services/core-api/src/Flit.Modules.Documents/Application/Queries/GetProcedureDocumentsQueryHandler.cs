using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Queries;

/// <summary>AC2 HU-9791 — GET /procedures/{id}/documents.</summary>
public sealed class GetProcedureDocumentsQueryHandler(IDocumentRepository repository)
{
    public async Task<Result<ProcedureDocumentsStatusDto, DocumentError>> HandleAsync(
        GetProcedureDocumentsQuery query, CancellationToken ct = default)
    {
        var procedure = await repository.FindProcedureAsync(query.ProcedureId, query.TenantId, ct);
        if (procedure is null)
            return Result<ProcedureDocumentsStatusDto, DocumentError>.Failure(DocumentError.ProcedureNotFound);

        var configs = await repository.GetAssociationsAsync(procedure.ProcedureTypeId, query.TenantId, ct);
        var documents = await repository.GetProcedureDocumentsAsync(query.ProcedureId, query.TenantId, ct);
        var packages = await repository.GetConsolidatedPackagesAsync(query.ProcedureId, query.TenantId, ct);

        var configByDocType = configs.ToDictionary(c => c.DocumentTypeId);
        var templateVersionById = new Dictionary<Guid, int>();

        foreach (var docTypeId in documents
                     .Where(d => d.TemplateVersionId is not null)
                     .Select(d => d.DocumentTypeId)
                     .Distinct())
        {
            var templates = await repository.GetTemplatesAsync(docTypeId, query.TenantId, ct);
            foreach (var template in templates)
                templateVersionById[template.Id] = template.Version;
        }

        var items = documents
            .Select(doc =>
            {
                configByDocType.TryGetValue(doc.DocumentTypeId, out var config);
                int? templateVersion = doc.TemplateVersionId is not null &&
                                       templateVersionById.TryGetValue(doc.TemplateVersionId.Value, out var ver)
                    ? ver
                    : null;

                return new ProcedureDocumentItemDto(
                    doc.Id,
                    doc.DocumentTypeId,
                    doc.DocumentType.Name,
                    doc.DocumentType.LoadType,
                    doc.Origin,
                    doc.Status,
                    templateVersion,
                    doc.FileRef,
                    doc.GeneratedAt,
                    doc.UploadedBy,
                    config?.IsRequired ?? false,
                    config?.OrderIndex ?? 0);
            })
            .OrderBy(d => d.OrderIndex)
            .ToList();

        var packageDtos = packages
            .Select(p => new ConsolidatedPackageDto(
                p.Version,
                p.MergedFileRef,
                p.CreatedAt,
                p.DownloadFilename,
                p.DocCount))
            .ToList();

        return Result<ProcedureDocumentsStatusDto, DocumentError>.Success(
            new ProcedureDocumentsStatusDto(query.ProcedureId, items, packageDtos));
    }
}
