using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

public sealed class DeleteOtOrganismCommandHandler(
    IOtOrganismRepository repository,
    IClock clock)
{
    public async Task<Result<bool, OtError>> HandleAsync(
        DeleteOtOrganismCommand command, CancellationToken ct = default)
    {
        var organism = await repository.FindByIdAsync(command.Id, command.TenantId, ct);
        if (organism is null)
            return Result<bool, OtError>.Failure(OtError.NotFound);

        await repository.SoftDeleteAsync(organism, command.RequestedByUserId, clock.UtcNow, ct);

        return Result<bool, OtError>.Success(true);
    }
}
