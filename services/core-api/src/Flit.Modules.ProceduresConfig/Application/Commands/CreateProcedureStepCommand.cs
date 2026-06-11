namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateProcedureStepCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Name,
    string StepType,
    int OrderIndex,
    bool IsRequired = true);
