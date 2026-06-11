namespace Flit.SharedKernel;

/// <summary>
/// Actualiza el estado de un trámite por composite_id (hot-update Quipux, HU-9800).
/// Implementado en Flit.Modules.Procedures; consumido por Flit.Modules.OT.
/// </summary>
public interface IProcedureStatusUpdater
{
  Task<Guid?> UpdateByCompositeIdAsync(
    Guid tenantId,
    string compositeId,
    string newStatus,
    CancellationToken ct = default);
}
