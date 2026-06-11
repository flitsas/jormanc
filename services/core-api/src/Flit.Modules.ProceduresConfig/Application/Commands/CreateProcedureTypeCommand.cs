namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateProcedureTypeCommand(
    Guid TenantId,
    Guid RequestedByUserId,
    string Name,
    string Family,
    string Scope,
    Guid? ScopeRefId,
    string VehicleQueryKey);
