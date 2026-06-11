using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

public sealed class UpdateOtOrganismCommandHandler(
    IOtOrganismRepository repository,
    IClock clock)
{
    public async Task<Result<OtOrganismDto, OtError>> HandleAsync(
        UpdateOtOrganismCommand command, CancellationToken ct = default)
    {
        var organism = await repository.FindByIdAsync(command.Id, command.TenantId, ct);
        if (organism is null)
            return Result<OtOrganismDto, OtError>.Failure(OtError.NotFound);

        var now = clock.UtcNow;
        organism.Name = command.Name.Trim();
        organism.UpdatedAt = now;
        organism.UpdatedBy = command.RequestedByUserId;

        await repository.UpdateAsync(organism, ct);

        return Result<OtOrganismDto, OtError>.Success(OtOrganismMapper.ToDto(organism));
    }
}
