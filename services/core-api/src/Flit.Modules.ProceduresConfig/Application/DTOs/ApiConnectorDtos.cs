namespace Flit.Modules.ProceduresConfig.Application.DTOs;

public sealed record ApiConnectorDto(
    Guid Id,
    Guid ProcedureTypeId,
    Guid TenantId,
    string Name,
    string Endpoint,
    string HttpVerb,
    int StepOrder,
    string ParamBindings,
    string ResponseMappings,
    bool IsActive,
    DateTimeOffset CreatedAt);
