using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed class CreateProcedureStepCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<ProcedureStepDto, ProcedureTypeError>> HandleAsync(
        CreateProcedureStepCommand command, CancellationToken ct = default)
    {
        var procedureType = await repository.FindByIdAsync(command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<ProcedureStepDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        var now = clock.UtcNow;
        var step = new ProcedureStep
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = command.ProcedureTypeId,
            TenantId = command.TenantId,
            OrderIndex = command.OrderIndex,
            Name = command.Name.Trim(),
            StepType = command.StepType.Trim().ToLowerInvariant(),
            IsRequired = command.IsRequired,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddStepAsync(step, ct);
        return Result<ProcedureStepDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToStepDto(step));
    }
}
