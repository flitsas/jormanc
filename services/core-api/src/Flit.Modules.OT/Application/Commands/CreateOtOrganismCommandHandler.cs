using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

/// <summary>
/// AC1 HU-9798 — POST /api/v1/ot-organisms crea OT con mode=dashboard, quipux_enabled=false.
/// </summary>
public sealed class CreateOtOrganismCommandHandler(
    IOtOrganismRepository repository,
    IClock clock)
{
    public async Task<Result<OtOrganismDto, OtError>> HandleAsync(
        CreateOtOrganismCommand command, CancellationToken ct = default)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();

        if (await repository.SlugExistsAsync(command.TenantId, slug, ct))
            return Result<OtOrganismDto, OtError>.Failure(OtError.SlugAlreadyExists);

        var now = clock.UtcNow;

        var organism = new OtOrganism
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Slug = slug,
            Name = command.Name.Trim(),
            Mode = "dashboard",
            QuipuxEnabled = false,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.CreateAsync(organism, ct);

        return Result<OtOrganismDto, OtError>.Success(OtOrganismMapper.ToDto(organism));
    }
}
