using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Domain.Services;
using Flit.Modules.Documents.Infrastructure.Templates;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC1 HU-9791 — generación automática tras ProcedureSubmitted y verificación de completitud.</summary>
public sealed class GenerateProcedureDocumentsCommandHandler(
    IDocumentRepository repository,
    IDocumentTemplateStorage templateStorage,
    IDocumentFileStorage fileStorage,
    ITemplateResolver templateResolver,
    IDocumentPdfRenderer pdfRenderer,
    ConsolidateDocumentsCommandHandler consolidateHandler,
    IClock clock)
{
    public async Task<Result<bool, DocumentError>> HandleAsync(
        GenerateProcedureDocumentsCommand command, CancellationToken ct = default)
    {
        var procedure = await repository.FindProcedureAsync(command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<bool, DocumentError>.Failure(DocumentError.ProcedureNotFound);

        var configs = await repository.GetAssociationsAsync(procedure.ProcedureTypeId, command.TenantId, ct);
        if (configs.Count == 0)
            return Result<bool, DocumentError>.Success(false);

        var actors = await repository.GetProcedureActorsAsync(command.ProcedureId, command.TenantId, ct);
        var actorDefs = await repository.GetActorDefinitionsAsync(procedure.ProcedureTypeId, command.TenantId, ct);
        var vehicleQuery = await repository.GetLatestVehicleQueryAsync(command.ProcedureId, command.TenantId, ct);
        var ot = procedure.OtId is null
            ? null
            : await repository.FindOtAsync(procedure.OtId.Value, command.TenantId, ct);

        var context = TemplateContextBuilder.Build(procedure, actors, actorDefs, vehicleQuery, ot);
        var now = clock.UtcNow;

        foreach (var config in configs)
        {
            var existing = await repository.FindProcedureDocumentAsync(
                command.ProcedureId, config.DocumentTypeId, command.TenantId, ct);

            if (config.DocumentType.LoadType == "generacion")
            {
                await GenerateDocumentAsync(
                    command, config, existing, context, now, ct);
            }
            else if (existing is null)
            {
                await repository.CreateProcedureDocumentAsync(new ProcedureDocument
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = command.ProcedureId,
                    DocumentTypeId = config.DocumentTypeId,
                    TenantId = command.TenantId,
                    Origin = "uploaded",
                    Status = "pending",
                    CreatedAt = now,
                    CreatedBy = command.UserId,
                    UpdatedAt = now,
                    UpdatedBy = command.UserId
                }, ct);
            }
        }

        var documents = await repository.GetProcedureDocumentsAsync(command.ProcedureId, command.TenantId, ct);
        if (!DocumentCompletionChecker.CanConsolidate(configs, documents))
            return Result<bool, DocumentError>.Success(false);

        var consolidateResult = await consolidateHandler.HandleAsync(
            new ConsolidateDocumentsCommand(command.ProcedureId, command.TenantId, command.UserId),
            ct);

        if (!consolidateResult.IsSuccess)
            return Result<bool, DocumentError>.Failure(consolidateResult.Error);

        return Result<bool, DocumentError>.Success(true);
    }

    private async Task GenerateDocumentAsync(
        GenerateProcedureDocumentsCommand command,
        ProcedureTypeDocument config,
        ProcedureDocument? existing,
        Domain.Models.TemplateContext context,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (existing is not null && existing.Status == "ready" && !string.IsNullOrWhiteSpace(existing.FileRef))
            return;

        var template = await repository.FindActiveTemplateAsync(config.DocumentTypeId, command.TenantId, ct);
        if (template is null)
        {
            if (existing is null)
            {
                await repository.CreateProcedureDocumentAsync(new ProcedureDocument
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = command.ProcedureId,
                    DocumentTypeId = config.DocumentTypeId,
                    TenantId = command.TenantId,
                    Origin = "generated",
                    Status = "failed",
                    CreatedAt = now,
                    CreatedBy = command.UserId,
                    UpdatedAt = now,
                    UpdatedBy = command.UserId
                }, ct);
            }

            return;
        }

        var html = await templateStorage.ReadTextAsync(template.ContentRef, ct);
        var resolved = templateResolver.Resolve(html, context);
        var pdf = pdfRenderer.RenderHtmlToPdf(resolved);

        var fileName = $"doc_{config.DocumentTypeId:N}_{template.Version}.pdf";
        var fileRef = $"procedures/{command.TenantId}/{command.ProcedureId}/generated/{fileName}";

        await using var pdfStream = new MemoryStream(pdf);
        await fileStorage.UploadAsync(fileRef, pdfStream, "application/pdf", ct);

        var metadata = JsonSerializer.Serialize(new
        {
            template_id = template.Id,
            template_version = template.Version,
            generated_at = now
        });

        if (existing is null)
        {
            await repository.CreateProcedureDocumentAsync(new ProcedureDocument
            {
                Id = Guid.NewGuid(),
                ProcedureId = command.ProcedureId,
                DocumentTypeId = config.DocumentTypeId,
                TenantId = command.TenantId,
                TemplateVersionId = template.Id,
                Origin = "generated",
                Status = "ready",
                FileRef = fileRef,
                FileName = fileName,
                GenerationMetadata = metadata,
                GeneratedAt = now,
                CreatedAt = now,
                CreatedBy = command.UserId,
                UpdatedAt = now,
                UpdatedBy = command.UserId
            }, ct);
        }
        else
        {
            existing.TemplateVersionId = template.Id;
            existing.Origin = "generated";
            existing.Status = "ready";
            existing.FileRef = fileRef;
            existing.FileName = fileName;
            existing.GenerationMetadata = metadata;
            existing.GeneratedAt = now;
            existing.UpdatedAt = now;
            existing.UpdatedBy = command.UserId;
            await repository.UpdateProcedureDocumentAsync(existing, ct);
        }
    }
}
