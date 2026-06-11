using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Queries;

public sealed class GetOtLabelImpactQueryHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentLabelRepository labelRepository)
{
    public async Task<Result<OtLabelImpactDto, OtError>> HandleAsync(
        GetOtLabelImpactQuery query, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(query.OtId, query.TenantId, ct);
        if (organism is null)
            return Result<OtLabelImpactDto, OtError>.Failure(OtError.NotFound);

        var label = await labelRepository.FindByIdAsync(query.LabelId, query.OtId, query.TenantId, ct);
        if (label is null)
            return Result<OtLabelImpactDto, OtError>.Failure(OtError.LabelNotFound);

        var impactCount = await labelRepository.CountAttachmentImpactAsync(
            query.OtId, label.Slug, query.TenantId, ct);

        return Result<OtLabelImpactDto, OtError>.Success(new OtLabelImpactDto(impactCount));
    }
}
