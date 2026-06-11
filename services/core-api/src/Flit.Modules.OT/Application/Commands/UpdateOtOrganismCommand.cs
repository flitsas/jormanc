namespace Flit.Modules.OT.Application.Commands;

public sealed record UpdateOtOrganismCommand(
    Guid Id,
    Guid TenantId,
    Guid RequestedByUserId,
    string Name);
