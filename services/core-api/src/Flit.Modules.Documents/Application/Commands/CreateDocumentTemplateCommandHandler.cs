using System.Text;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Domain.Services;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC1–AC2 HU-9790 — POST /document-types/{id}/templates</summary>
public sealed class CreateDocumentTemplateCommandHandler(
    IDocumentRepository repository,
    IDocumentTemplateStorage storage,
    ITemplateResolver templateResolver,
    IClock clock)
{
    public async Task<Result<DocumentTemplateDto, DocumentError>> HandleAsync(
        CreateDocumentTemplateCommand command, CancellationToken ct = default)
    {
        if (command.HtmlStream is null || !command.HtmlStream.CanRead)
            return Result<DocumentTemplateDto, DocumentError>.Failure(DocumentError.TemplateHtmlRequired);

        var documentType = await repository.FindDocumentTypeByIdAsync(
            command.DocumentTypeId, command.TenantId, ct);

        if (documentType is null)
            return Result<DocumentTemplateDto, DocumentError>.Failure(DocumentError.DocumentTypeNotFound);

        if (!string.Equals(documentType.LoadType, "generacion", StringComparison.Ordinal))
            return Result<DocumentTemplateDto, DocumentError>.Failure(DocumentError.TemplateRequiresGeneracionLoadType);

        string rawHtml;
        using (var reader = new StreamReader(command.HtmlStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            rawHtml = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(rawHtml))
            return Result<DocumentTemplateDto, DocumentError>.Failure(DocumentError.TemplateHtmlEmpty);

        var sanitizedHtml = HtmlSanitizer.Sanitize(rawHtml);
        var markers = templateResolver.DetectMarkers(sanitizedHtml);

        await repository.DeprecateActiveTemplatesAsync(command.DocumentTypeId, command.TenantId, ct);

        var nextVersion = await repository.GetMaxTemplateVersionAsync(command.DocumentTypeId, command.TenantId, ct) + 1;
        var contentRef = $"templates/{command.TenantId}/{command.DocumentTypeId}/{nextVersion}.html";

        await using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(sanitizedHtml));
        await storage.UploadAsync(contentRef, uploadStream, "text/html; charset=utf-8", ct);

        var now = clock.UtcNow;
        var templateId = Guid.NewGuid();
        var template = new DocumentTemplate
        {
            Id = templateId,
            DocumentTypeId = command.DocumentTypeId,
            TenantId = command.TenantId,
            Version = nextVersion,
            ContentRef = contentRef,
            Status = "active",
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            CreatedBy = command.RequestedByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var fields = markers.Select(marker =>
        {
            var (dataSource, dataPath) = MarkerMetadataParser.Parse(marker);
            return new TemplateField
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Marker = marker,
                DataSource = dataSource,
                DataPath = dataPath,
                IsRequired = false,
                CreatedAt = now
            };
        }).ToArray();

        await repository.CreateTemplateAsync(template, fields, ct);

        return Result<DocumentTemplateDto, DocumentError>.Success(new DocumentTemplateDto(
            templateId,
            command.DocumentTypeId,
            nextVersion,
            "active",
            contentRef,
            template.Notes,
            markers,
            now));
    }
}
