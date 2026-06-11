namespace Flit.Modules.Documents.Application.Commands;

public sealed record CreateDocumentTemplateCommand(
    Guid DocumentTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    Stream HtmlStream,
    string? Notes);
