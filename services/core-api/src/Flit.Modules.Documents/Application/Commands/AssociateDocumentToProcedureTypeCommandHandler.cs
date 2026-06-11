using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC1/AC2 HU-9789 — POST /procedure-types/{id}/document-config</summary>
public sealed class AssociateDocumentToProcedureTypeCommandHandler(
    IDocumentRepository repository,
    IClock clock)
{
    public async Task<Result<ProcedureTypeDocumentConfigDto, DocumentError>> HandleAsync(
        AssociateDocumentToProcedureTypeCommand command, CancellationToken ct = default)
    {
        if (!await repository.ProcedureTypeExistsAsync(command.ProcedureTypeId, command.TenantId, ct))
            return Result<ProcedureTypeDocumentConfigDto, DocumentError>.Failure(DocumentError.ProcedureTypeNotFound);

        var documentType = await repository.FindDocumentTypeByIdAsync(command.DocumentTypeId, command.TenantId, ct);
        if (documentType is null)
            return Result<ProcedureTypeDocumentConfigDto, DocumentError>.Failure(DocumentError.DocumentTypeNotFound);

        if (await repository.AssociationExistsAsync(
                command.ProcedureTypeId, command.DocumentTypeId, command.TenantId, ct))
        {
            return Result<ProcedureTypeDocumentConfigDto, DocumentError>.Failure(
                DocumentError.DocumentAlreadyAssociated);
        }

        if (command.ActorDefinitionId is Guid actorId &&
            !await repository.ActorDefinitionBelongsToProcedureTypeAsync(
                actorId, command.ProcedureTypeId, command.TenantId, ct))
        {
            return Result<ProcedureTypeDocumentConfigDto, DocumentError>.Failure(
                DocumentError.ActorDefinitionNotFound);
        }

        var now = clock.UtcNow;
        var entity = new ProcedureTypeDocument
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = command.ProcedureTypeId,
            DocumentTypeId = command.DocumentTypeId,
            TenantId = command.TenantId,
            IsRequired = command.IsRequired,
            OrderIndex = command.OrderIndex,
            ActorDefinitionId = command.ActorDefinitionId,
            AllowPartialConsolidation = command.AllowPartialConsolidation,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.CreateAssociationAsync(entity, ct);

        return Result<ProcedureTypeDocumentConfigDto, DocumentError>.Success(
            MapToDto(entity, documentType));
    }

    internal static ProcedureTypeDocumentConfigDto MapToDto(
        ProcedureTypeDocument entity,
        DocumentType? documentType = null)
    {
        var docType = documentType ?? entity.DocumentType;
        return new(
            entity.Id,
            entity.ProcedureTypeId,
            entity.DocumentTypeId,
            docType.Name,
            docType.LoadType,
            entity.IsRequired,
            entity.OrderIndex,
            entity.ActorDefinitionId,
            entity.AllowPartialConsolidation,
            entity.CreatedAt);
    }
}
