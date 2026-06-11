namespace Flit.Modules.OT.Application.Commands;

public sealed record UpdateQuipuxConfigCommand(
    Guid Id,
    Guid TenantId,
    Guid RequestedByUserId,
    string Endpoint,
    string? WebhookToken);
