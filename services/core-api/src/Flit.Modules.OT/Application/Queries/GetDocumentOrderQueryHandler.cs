using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Queries;

public sealed class GetDocumentOrderQueryHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentOrderRepository orderRepository,
    IOtDocumentTypeLookup documentTypeLookup)
{
    public async Task<Result<IReadOnlyList<DocumentOrderEntryDto>, OtError>> HandleAsync(
        GetDocumentOrderQuery query, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(query.OtId, query.TenantId, ct);
        if (organism is null)
            return Result<IReadOnlyList<DocumentOrderEntryDto>, OtError>.Failure(OtError.NotFound);

        var orders = await orderRepository.ListByOtAsync(query.OtId, query.TenantId, ct);
        var allIds = orders
            .SelectMany(o => OtDocumentMapper.DeserializeOrderedIds(o.OrderedDocumentTypeIds))
            .Distinct()
            .ToList();

        var documentTypes = await documentTypeLookup.GetByIdsAsync(query.TenantId, allIds, ct);

        var entries = orders.Select(order =>
        {
            var orderedIds = OtDocumentMapper.DeserializeOrderedIds(order.OrderedDocumentTypeIds);
            return new DocumentOrderEntryDto(
                order.ProcedureTypeId,
                OtDocumentMapper.MapOrderedDocuments(orderedIds, documentTypes));
        }).ToList();

        return Result<IReadOnlyList<DocumentOrderEntryDto>, OtError>.Success(entries);
    }
}
