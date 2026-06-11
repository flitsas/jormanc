using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

/// <summary>
/// AC1 HU-9799 — PUT document-order ejecuta UPSERT JSONB en ot_document_orders.
/// </summary>
public sealed class UpdateDocumentOrderCommandHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentOrderRepository orderRepository,
    IOtDocumentTypeLookup documentTypeLookup,
    IClock clock)
{
    public async Task<Result<UpdateDocumentOrderResultDto, OtError>> HandleAsync(
        UpdateDocumentOrderCommand command, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(command.OtId, command.TenantId, ct);
        if (organism is null)
            return Result<UpdateDocumentOrderResultDto, OtError>.Failure(OtError.NotFound);

        var procedureTypeExists = await documentTypeLookup.ProcedureTypeExistsAsync(
            command.ProcedureTypeId, command.TenantId, ct);

        if (!procedureTypeExists)
            return Result<UpdateDocumentOrderResultDto, OtError>.Failure(OtError.ProcedureTypeNotFound);

        var now = clock.UtcNow;
        var serialized = OtDocumentMapper.SerializeOrderedIds(command.OrderedDocumentTypeIds);

        var order = new OtDocumentOrder
        {
            Id = Guid.NewGuid(),
            OtId = command.OtId,
            ProcedureTypeId = command.ProcedureTypeId,
            TenantId = command.TenantId,
            OrderedDocumentTypeIds = serialized,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await orderRepository.UpsertAsync(order, ct);

        var documentTypes = await documentTypeLookup.GetByIdsAsync(
            command.TenantId, command.OrderedDocumentTypeIds, ct);

        var orderedDocuments = OtDocumentMapper.MapOrderedDocuments(
            command.OrderedDocumentTypeIds, documentTypes);

        return Result<UpdateDocumentOrderResultDto, OtError>.Success(
            new UpdateDocumentOrderResultDto(command.ProcedureTypeId, orderedDocuments, true));
    }
}
