using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

public sealed class CreateOtLabelCommandHandler(
    IOtOrganismRepository organismRepository,
    IOtDocumentLabelRepository labelRepository,
    IClock clock)
{
    public async Task<Result<OtDocumentLabelDto, OtError>> HandleAsync(
        CreateOtLabelCommand command, CancellationToken ct = default)
    {
        var organism = await organismRepository.FindByIdAsync(command.OtId, command.TenantId, ct);
        if (organism is null)
            return Result<OtDocumentLabelDto, OtError>.Failure(OtError.NotFound);

        var slug = command.Slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
            return Result<OtDocumentLabelDto, OtError>.Failure(
                new OtError("VALIDATION_ERROR", "slug es requerido."));

        if (await labelRepository.SlugExistsAsync(command.OtId, slug, command.TenantId, ct))
            return Result<OtDocumentLabelDto, OtError>.Failure(OtError.LabelSlugAlreadyExists);

        var now = clock.UtcNow;
        var label = new OtDocumentLabel
        {
            Id = Guid.NewGuid(),
            OtId = command.OtId,
            TenantId = command.TenantId,
            Slug = slug,
            DisplayName = command.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await labelRepository.CreateAsync(label, ct);

        return Result<OtDocumentLabelDto, OtError>.Success(OtDocumentMapper.ToLabelDto(label));
    }
}
