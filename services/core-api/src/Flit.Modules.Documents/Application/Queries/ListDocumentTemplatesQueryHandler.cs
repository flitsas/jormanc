using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Queries;

public sealed class ListDocumentTemplatesQueryHandler(IDocumentRepository repository)
{
    public async Task<Result<IReadOnlyList<DocumentTemplateDto>, DocumentError>> HandleAsync(
        ListDocumentTemplatesQuery query, CancellationToken ct = default)
    {
        var documentType = await repository.FindDocumentTypeByIdAsync(
            query.DocumentTypeId, query.TenantId, ct);

        if (documentType is null)
            return Result<IReadOnlyList<DocumentTemplateDto>, DocumentError>.Failure(DocumentError.DocumentTypeNotFound);

        var templates = await repository.GetTemplatesAsync(query.DocumentTypeId, query.TenantId, ct);
        var dtos = templates.Select(t => new DocumentTemplateDto(
            t.Id,
            t.DocumentTypeId,
            t.Version,
            t.Status,
            t.ContentRef,
            t.Notes,
            t.Fields.Select(f => f.Marker).OrderBy(m => m, StringComparer.Ordinal).ToArray(),
            t.CreatedAt)).ToArray();

        return Result<IReadOnlyList<DocumentTemplateDto>, DocumentError>.Success(dtos);
    }
}
