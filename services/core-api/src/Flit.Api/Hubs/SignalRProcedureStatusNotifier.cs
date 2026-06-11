using Flit.SharedKernel;
using Microsoft.AspNetCore.SignalR;

namespace Flit.Api.Hubs;

/// <summary>
/// SignalR — evento procedure_status_update para hot-update Quipux (HU-9800).
/// </summary>
public sealed class SignalRProcedureStatusNotifier(IHubContext<SessionHub> hubContext)
  : IProcedureStatusNotifier
{
  public Task NotifyStatusUpdateAsync(
    Guid tenantId,
    Guid procedureId,
    string compositeId,
    string newStatus,
    CancellationToken ct = default)
    => hubContext.Clients
      .Group(TenantGroup(tenantId))
      .SendAsync(
        "procedure_status_update",
        new { procedure_id = procedureId, composite_id = compositeId, status = newStatus },
        cancellationToken: ct);

  internal static string TenantGroup(Guid tenantId) => $"tenant:{tenantId}";
}
