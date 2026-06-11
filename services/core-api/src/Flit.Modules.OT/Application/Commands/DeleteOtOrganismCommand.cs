namespace Flit.Modules.OT.Application.Commands;

public sealed record DeleteOtOrganismCommand(
    Guid Id,
    Guid TenantId,
    Guid RequestedByUserId);
