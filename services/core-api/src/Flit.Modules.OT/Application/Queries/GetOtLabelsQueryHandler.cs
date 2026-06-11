using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Queries;

public sealed class GetOtLabelsQueryHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentLabelRepository labelRepository)
{
    public async Task<Result<IReadOnlyList<OtDocumentLabelDto>, OtError>> HandleAsync(
        GetOtLabelsQuery query, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(query.OtId, query.TenantId, ct);
        if (organism is null)
            return Result<IReadOnlyList<OtDocumentLabelDto>, OtError>.Failure(OtError.NotFound);

        var labels = await labelRepository.ListByOtAsync(query.OtId, query.TenantId, ct);
        var dtos = labels.Select(OtDocumentMapper.ToLabelDto).ToList();

        return Result<IReadOnlyList<OtDocumentLabelDto>, OtError>.Success(dtos);
    }
}
