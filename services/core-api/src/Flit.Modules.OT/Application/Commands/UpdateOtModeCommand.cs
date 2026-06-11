namespace Flit.Modules.OT.Application.Commands;

public sealed record UpdateOtModeCommand(
    Guid Id,
    Guid TenantId,
    Guid RequestedByUserId,
    string Mode);
