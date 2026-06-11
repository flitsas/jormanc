using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC3 HU-9790 — resolución de marcadores + PDF QuestPDF de prueba.</summary>
public sealed class GenerateTemplatePreviewPdfCommandHandler(
    IDocumentRepository repository,
    IDocumentTemplateStorage storage,
    ITemplateResolver templateResolver,
    IDocumentPdfRenderer pdfRenderer)
{
    public async Task<Result<byte[], DocumentError>> HandleAsync(
        GenerateTemplatePreviewPdfCommand command, CancellationToken ct = default)
    {
        var documentType = await repository.FindDocumentTypeByIdAsync(
            command.DocumentTypeId, command.TenantId, ct);

        if (documentType is null)
            return Result<byte[], DocumentError>.Failure(DocumentError.DocumentTypeNotFound);

        var template = await repository.FindTemplateByIdAsync(
            command.TemplateId, command.DocumentTypeId, command.TenantId, ct);

        if (template is null)
            return Result<byte[], DocumentError>.Failure(DocumentError.TemplateNotFound);

        var html = await storage.ReadTextAsync(template.ContentRef, ct);
        var resolved = templateResolver.Resolve(html, command.Context);
        var pdf = pdfRenderer.RenderHtmlToPdf(resolved);

        return Result<byte[], DocumentError>.Success(pdf);
    }
}
