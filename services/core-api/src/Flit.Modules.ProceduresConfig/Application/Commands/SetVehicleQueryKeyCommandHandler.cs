using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC3 HU-9781 — PUT /procedure-types/{id}/vehicle-query</summary>
public sealed class SetVehicleQueryKeyCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidKeys = ["placa", "vin", "placa_vin"];

    public async Task<Result<ProcedureTypeDto, ProcedureTypeError>> HandleAsync(
        SetVehicleQueryKeyCommand command, CancellationToken ct = default)
    {
        var key = command.QueryKey.Trim().ToLowerInvariant();
        if (!ValidKeys.Contains(key))
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidVehicleQueryKey);

        var procedureType = await repository.FindByIdAsync(command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        var now = clock.UtcNow;
        await repository.UpdateVehicleQueryKeyAsync(procedureType, key, command.RequestedByUserId, now, ct);

        return Result<ProcedureTypeDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToDto(procedureType));
    }
}
