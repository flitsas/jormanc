using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

public sealed class DeleteOtLabelCommandHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentLabelRepository labelRepository)
{
    public async Task<Result<DeleteOtLabelResultDto, OtError>> HandleAsync(
        DeleteOtLabelCommand command, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(command.OtId, command.TenantId, ct);
        if (organism is null)
            return Result<DeleteOtLabelResultDto, OtError>.Failure(OtError.NotFound);

        var label = await labelRepository.FindByIdAsync(command.LabelId, command.OtId, command.TenantId, ct);
        if (label is null)
            return Result<DeleteOtLabelResultDto, OtError>.Failure(OtError.LabelNotFound);

        var impactCount = await labelRepository.CountAttachmentImpactAsync(
            command.OtId, label.Slug, command.TenantId, ct);

        if (impactCount > 0 && !command.Confirm)
            return Result<DeleteOtLabelResultDto, OtError>.Failure(OtError.LabelInUse(impactCount));

        await labelRepository.DeleteAsync(label, ct);

        return Result<DeleteOtLabelResultDto, OtError>.Success(
            new DeleteOtLabelResultDto(true, impactCount));
    }
}
