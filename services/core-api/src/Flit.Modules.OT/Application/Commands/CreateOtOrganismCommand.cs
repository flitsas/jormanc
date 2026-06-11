namespace Flit.Modules.OT.Application.Commands;

public sealed record CreateOtOrganismCommand(
    Guid TenantId,
    Guid RequestedByUserId,
    string Slug,
    string Name);
