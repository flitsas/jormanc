using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC3 HU-9779 — DELETE bloqueado si hay trámites draft/submitted</summary>
public sealed class DeleteProcedureTypeCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<Unit, ProcedureTypeError>> HandleAsync(
        DeleteProcedureTypeCommand command, CancellationToken ct = default)
    {
        var entity = await repository.FindByIdAsync(command.Id, command.TenantId, ct);
        if (entity is null)
            return Result<Unit, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        if (await repository.HasActiveProceduresAsync(command.Id, ct))
            return Result<Unit, ProcedureTypeError>.Failure(ProcedureTypeError.HasActiveProcedures);

        await repository.SoftDeleteAsync(entity, command.RequestedByUserId, clock.UtcNow, ct);
        return Result<Unit, ProcedureTypeError>.Success(default);
    }
}

public readonly struct Unit;
