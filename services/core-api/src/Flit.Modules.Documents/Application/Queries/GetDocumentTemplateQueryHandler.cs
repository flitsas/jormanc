using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Queries;

public sealed class GetDocumentTemplateQueryHandler(
    IDocumentRepository repository,
    IDocumentTemplateStorage storage)
{
    public async Task<Result<DocumentTemplateDetailDto, DocumentError>> HandleAsync(
        GetDocumentTemplateQuery query, CancellationToken ct = default)
    {
        var documentType = await repository.FindDocumentTypeByIdAsync(
            query.DocumentTypeId, query.TenantId, ct);

        if (documentType is null)
            return Result<DocumentTemplateDetailDto, DocumentError>.Failure(DocumentError.DocumentTypeNotFound);

        var template = await repository.FindTemplateByIdAsync(
            query.TemplateId, query.DocumentTypeId, query.TenantId, ct);

        if (template is null)
            return Result<DocumentTemplateDetailDto, DocumentError>.Failure(DocumentError.TemplateNotFound);

        var html = await storage.ReadTextAsync(template.ContentRef, ct);
        var markers = template.Fields.Select(f => f.Marker).OrderBy(m => m, StringComparer.Ordinal).ToArray();

        return Result<DocumentTemplateDetailDto, DocumentError>.Success(new DocumentTemplateDetailDto(
            template.Id,
            template.DocumentTypeId,
            template.Version,
            template.Status,
            template.ContentRef,
            html,
            markers,
            template.Notes,
            template.CreatedAt));
    }
}
