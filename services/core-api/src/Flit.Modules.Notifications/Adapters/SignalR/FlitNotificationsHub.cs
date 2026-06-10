using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Flit.Modules.Notifications.Adapters.SignalR;

/// <summary>
/// Hub SignalR para notificaciones push en tiempo real al frontend.
/// Reemplaza el módulo WebSockets de services/node-bff/ (eliminado por ADR-0014).
///
/// El frontend se conecta a /hubs/notifications (proxeado por Flit.Gateway/YARP).
/// Grupos por user_id permiten dirigir notificaciones a usuarios específicos.
/// </summary>
[Authorize]
public sealed class FlitNotificationsHub : Hub<IFlitNotificationsClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}

public interface IFlitNotificationsClient
{
    Task ProcedureStateChanged(ProcedureStateChangedNotification notification);
    Task FileUploaded(FileUploadedNotification notification);
    Task ReceiptGenerated(ReceiptGeneratedNotification notification);
}

public sealed record ProcedureStateChangedNotification(
    Guid ProcedureId,
    string ProcedureCode,
    string NewStatus,
    DateTimeOffset At);

public sealed record FileUploadedNotification(
    Guid FileId,
    string Filename,
    string Scope,
    Guid ScopeId,
    DateTimeOffset At);

public sealed record ReceiptGeneratedNotification(
    Guid ProcedureId,
    Guid ReceiptFileId,
    DateTimeOffset At);
