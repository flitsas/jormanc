using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

/// <summary>
/// AC2 HU-9798 — PATCH /api/v1/ot-organisms/{id}/mode alterna dashboard ↔ qx.
/// </summary>
public sealed class UpdateOtModeCommandHandler(
    IOtOrganismRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> AllowedModes =
        new(StringComparer.OrdinalIgnoreCase) { "dashboard", "qx" };

    public async Task<Result<OtOrganismDto, OtError>> HandleAsync(
        UpdateOtModeCommand command, CancellationToken ct = default)
    {
        var mode = command.Mode.Trim().ToLowerInvariant();
        if (!AllowedModes.Contains(mode))
            return Result<OtOrganismDto, OtError>.Failure(OtError.InvalidMode);

        var organism = await repository.FindByIdAsync(command.Id, command.TenantId, ct);
        if (organism is null)
            return Result<OtOrganismDto, OtError>.Failure(OtError.NotFound);

        var now = clock.UtcNow;
        organism.Mode = mode;
        organism.QuipuxEnabled = mode == "qx";
        organism.UpdatedAt = now;
        organism.UpdatedBy = command.RequestedByUserId;

        await repository.UpdateAsync(organism, ct);

        return Result<OtOrganismDto, OtError>.Success(OtOrganismMapper.ToDto(organism));
    }
}
