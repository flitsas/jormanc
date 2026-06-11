namespace Flit.SharedKernel;

/// <summary>
/// Notifica en tiempo real un cambio de estado de trámite (SignalR, HU-9800).
/// Implementado en Flit.Api; consumido por Flit.Modules.OT.
/// </summary>
public interface IProcedureStatusNotifier
{
  Task NotifyStatusUpdateAsync(
    Guid tenantId,
    Guid procedureId,
    string compositeId,
    string newStatus,
    CancellationToken ct = default);
}
