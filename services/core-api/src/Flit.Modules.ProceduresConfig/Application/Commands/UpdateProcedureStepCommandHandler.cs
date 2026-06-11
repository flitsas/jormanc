using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC1 HU-9782 — PUT step order_index</summary>
public sealed class UpdateProcedureStepCommandHandler(IProcedureTypeRepository repository)
{
    public async Task<Result<ProcedureStepDto, ProcedureTypeError>> HandleAsync(
        UpdateProcedureStepCommand command, CancellationToken ct = default)
    {
        var step = await repository.FindStepAsync(command.ProcedureTypeId, command.StepId, command.TenantId, ct);
        if (step is null)
            return Result<ProcedureStepDto, ProcedureTypeError>.Failure(ProcedureTypeError.StepNotFound);

        if (!string.IsNullOrWhiteSpace(command.Name))
            step.Name = command.Name.Trim();

        await repository.UpdateStepOrderAsync(
            command.ProcedureTypeId, command.StepId, command.OrderIndex, command.TenantId, ct);

        step = await repository.FindStepAsync(command.ProcedureTypeId, command.StepId, command.TenantId, ct);
        return Result<ProcedureStepDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToStepDto(step!));
    }
}
