namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record SetVehicleQueryKeyCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    string QueryKey);
