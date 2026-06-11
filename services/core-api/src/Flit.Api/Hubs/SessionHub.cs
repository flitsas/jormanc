using Microsoft.AspNetCore.SignalR;

namespace Flit.Api.Hubs;

/// <summary>
/// SignalR Hub para notificaciones de sesión en tiempo real (ADR-0013).
/// Los eventos son exclusivamente server-pushed — no hay métodos invocables desde el cliente.
/// El frontend se conecta aquí para recibir el evento "SessionRevoked" y redirigir al login.
/// Ruta: /hubs/session
/// </summary>
public sealed class SessionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tid")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRProcedureStatusNotifier.TenantGroup(Guid.Parse(tenantId)));

        await base.OnConnectedAsync();
    }
}
