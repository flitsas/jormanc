namespace Flit.Modules.Documents.Application.Queries;

public sealed record GetDocumentTemplateQuery(
    Guid DocumentTypeId,
    Guid TemplateId,
    Guid TenantId);
