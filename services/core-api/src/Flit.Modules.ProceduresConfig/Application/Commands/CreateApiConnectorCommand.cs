namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateApiConnectorCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Name,
    string Endpoint,
    string HttpVerb,
    int StepOrder,
    string ParamBindingsJson);
