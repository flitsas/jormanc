using Flit.Modules.Documents.Domain.Models;

namespace Flit.Modules.Documents.Application.Commands;

public sealed record GenerateTemplatePreviewPdfCommand(
    Guid DocumentTypeId,
    Guid TemplateId,
    Guid TenantId,
    TemplateContext Context);
