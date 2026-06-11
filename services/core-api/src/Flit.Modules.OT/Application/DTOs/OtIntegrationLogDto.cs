namespace Flit.Modules.OT.Application.DTOs;

public sealed record OtIntegrationLogDto(
  Guid Id,
  Guid OtId,
  string EventType,
  string? ProcedureRef,
  string? RequestPayload,
  string? ResponsePayload,
  int? HttpStatus,
  int? DurationMs,
  DateTimeOffset LoggedAt);

public sealed record OtIntegrationLogsPageDto(
  IReadOnlyList<OtIntegrationLogDto> Data,
  int Total,
  int Page,
  int PageSize);
