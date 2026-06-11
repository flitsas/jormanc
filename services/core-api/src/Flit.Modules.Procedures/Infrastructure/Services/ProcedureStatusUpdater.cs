using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Infrastructure.Services;

/// <summary>
/// Actualiza procedures.status por composite_id (webhook Quipux, HU-9800).
/// </summary>
public sealed class ProcedureStatusUpdater(
  IProcedureRepository procedureRepository,
  IClock clock) : IProcedureStatusUpdater
{
  public async Task<Guid?> UpdateByCompositeIdAsync(
    Guid tenantId,
    string compositeId,
    string newStatus,
    CancellationToken ct = default)
  {
    var procedure = await procedureRepository.FindByCompositeIdAsync(compositeId, tenantId, ct);
    if (procedure is null)
      return null;

    var now = clock.UtcNow;
    procedure.Status = newStatus;
    procedure.UpdatedAt = now;

    if (newStatus == "approved")
      procedure.ApprovedAt = now;

    await procedureRepository.UpdateAsync(procedure, ct);
    return procedure.Id;
  }
}
