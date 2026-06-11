using Flit.Infrastructure.Persistence.Entities.OT;

namespace Flit.Modules.OT.Domain.Interfaces;

public sealed record OtIntegrationLogFilter(
  Guid TenantId,
  Guid OtId,
  int Page,
  int PageSize,
  string? EventType = null,
  DateTimeOffset? From = null,
  DateTimeOffset? To = null);

/// <summary>
/// Repositorio de ot_integration_logs (inmutable — solo INSERT y lectura).
/// </summary>
public interface IOtIntegrationLogRepository
{
  Task AddAsync(OtIntegrationLog log, CancellationToken ct = default);

  Task<(IReadOnlyList<OtIntegrationLog> Items, int Total)> ListAsync(
    OtIntegrationLogFilter filter,
    CancellationToken ct = default);
}
