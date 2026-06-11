namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateRuleSetCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Name,
    string ConditionsJson,
    string ActionsJson);
