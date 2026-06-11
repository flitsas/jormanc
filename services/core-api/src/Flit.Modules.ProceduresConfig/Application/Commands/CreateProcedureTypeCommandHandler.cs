using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Helpers;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC1 HU-9779 — POST /procedure-types</summary>
public sealed class CreateProcedureTypeCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidFamilies =
        ["matricula_inicial", "traspasos", "otros"];

    private static readonly HashSet<string> ValidScopes =
        ["global", "company", "ot", "company_ot"];

    private static readonly HashSet<string> ValidVehicleKeys =
        ["placa", "vin", "placa_vin"];

    public async Task<Result<ProcedureTypeDto, ProcedureTypeError>> HandleAsync(
        CreateProcedureTypeCommand command, CancellationToken ct = default)
    {
        if (!ValidFamilies.Contains(command.Family))
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidFamily);

        if (!ValidScopes.Contains(command.Scope))
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidScope);

        if (!ValidVehicleKeys.Contains(command.VehicleQueryKey))
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidVehicleQueryKey);

        var slug = SlugHelper.FromName(command.Name);
        if (await repository.SlugExistsAsync(command.TenantId, slug, ct))
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.SlugAlreadyExists);

        var now = clock.UtcNow;
        var entity = new ProcedureType
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Slug = slug,
            Name = command.Name.Trim(),
            Family = command.Family,
            Scope = command.Scope,
            ScopeRefId = command.ScopeRefId,
            VehicleQueryKey = command.VehicleQueryKey,
            Version = 1,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.CreateAsync(entity, ct);
        return Result<ProcedureTypeDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToDto(entity));
    }
}
