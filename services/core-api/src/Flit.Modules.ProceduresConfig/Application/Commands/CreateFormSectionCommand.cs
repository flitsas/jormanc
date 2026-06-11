namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateFormSectionCommand(
    Guid ProcedureTypeId,
    Guid StepId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Slug,
    string Name,
    int OrderIndex,
    bool IsCollapsible = false);
