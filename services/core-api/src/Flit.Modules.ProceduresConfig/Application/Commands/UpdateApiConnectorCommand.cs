namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record UpdateApiConnectorCommand(
    Guid ProcedureTypeId,
    Guid ConnectorId,
    Guid TenantId,
    Guid RequestedByUserId,
    string ParamBindingsJson);
