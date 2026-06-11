using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Flit.Api.Hubs;

/// <summary>
/// Implementación de ISessionNotifier que usa SignalR para notificar al cliente
/// que su sesión fue revocada (ADR-0013, AC2 HU-9771).
/// IHubContext&lt;T&gt; es singleton → este servicio puede ser scoped.
/// Envía el evento "SessionRevoked" al canal del usuario afectado.
/// </summary>
public sealed class SignalRSessionNotifier(IHubContext<SessionHub> hubContext) : ISessionNotifier
{
    public Task NotifySessionRevokedAsync(Guid userId, string reason, CancellationToken ct = default)
        => hubContext.Clients
            .User(userId.ToString())
            .SendAsync("SessionRevoked", new { reason }, cancellationToken: ct);
}
