using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

public sealed class UpdateOtLabelCommandHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentLabelRepository labelRepository,
    IClock clock)
{
    public async Task<Result<OtDocumentLabelDto, OtError>> HandleAsync(
        UpdateOtLabelCommand command, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(command.OtId, command.TenantId, ct);
        if (organism is null)
            return Result<OtDocumentLabelDto, OtError>.Failure(OtError.NotFound);

        var label = await labelRepository.FindByIdAsync(command.LabelId, command.OtId, command.TenantId, ct);
        if (label is null)
            return Result<OtDocumentLabelDto, OtError>.Failure(OtError.LabelNotFound);

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            return Result<OtDocumentLabelDto, OtError>.Failure(
                new OtError("VALIDATION_ERROR", "display_name es requerido."));

        var now = clock.UtcNow;
        label.DisplayName = command.DisplayName.Trim();
        if (command.IsActive.HasValue)
            label.IsActive = command.IsActive.Value;
        label.UpdatedAt = now;
        label.UpdatedBy = command.RequestedByUserId;

        await labelRepository.UpdateAsync(label, ct);

        return Result<OtDocumentLabelDto, OtError>.Success(OtDocumentMapper.ToLabelDto(label));
    }
}
