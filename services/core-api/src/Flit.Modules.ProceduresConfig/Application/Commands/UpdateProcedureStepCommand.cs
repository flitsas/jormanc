namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record UpdateProcedureStepCommand(
    Guid ProcedureTypeId,
    Guid StepId,
    Guid TenantId,
    Guid RequestedByUserId,
    int OrderIndex,
    string? Name);
