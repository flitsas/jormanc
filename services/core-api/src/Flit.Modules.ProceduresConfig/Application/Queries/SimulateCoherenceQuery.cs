namespace Flit.Modules.ProceduresConfig.Application.Queries;

public sealed record SimulateCoherenceQuery(
    Guid ProcedureTypeId,
    Guid TenantId,
    string Name,
    string ConditionsJson,
    string ActionsJson);
