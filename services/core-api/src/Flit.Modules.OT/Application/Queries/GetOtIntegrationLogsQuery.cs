namespace Flit.Modules.OT.Application.Queries;

/// <summary>
/// AC3 HU-9800 — listado paginado de ot_integration_logs.
/// </summary>
public sealed record GetOtIntegrationLogsQuery(
  Guid OtId,
  Guid TenantId,
  int Page = 1,
  int PageSize = 20,
  string? EventType = null,
  DateTimeOffset? From = null,
  DateTimeOffset? To = null);
