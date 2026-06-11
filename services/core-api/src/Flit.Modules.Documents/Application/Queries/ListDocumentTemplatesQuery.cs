namespace Flit.Modules.Documents.Application.Queries;

public sealed record ListDocumentTemplatesQuery(Guid DocumentTypeId, Guid TenantId);
