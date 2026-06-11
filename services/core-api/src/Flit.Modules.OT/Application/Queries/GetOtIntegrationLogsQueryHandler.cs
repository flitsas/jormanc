using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Queries;

/// <summary>
/// AC3 HU-9800 — logs Quipux inmutables y paginables por event_type.
/// </summary>
public sealed class GetOtIntegrationLogsQueryHandler(
  IOtOrganismRepository organismRepository,
  IOtIntegrationLogRepository logRepository)
{
  public async Task<Result<OtIntegrationLogsPageDto, OtError>> HandleAsync(
    GetOtIntegrationLogsQuery query,
    CancellationToken ct = default)
  {
    var organism = await organismRepository.FindByIdAsync(query.OtId, query.TenantId, ct);
    if (organism is null)
      return Result<OtIntegrationLogsPageDto, OtError>.Failure(OtError.NotFound);

    var effectivePage = query.Page < 1 ? 1 : query.Page;
    var effectivePageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

    var filter = new OtIntegrationLogFilter(
      TenantId: query.TenantId,
      OtId: query.OtId,
      Page: effectivePage,
      PageSize: effectivePageSize,
      EventType: query.EventType,
      From: query.From,
      To: query.To);

    var (items, total) = await logRepository.ListAsync(filter, ct);

    var data = items.Select(l => new OtIntegrationLogDto(
      l.Id,
      l.OtId,
      l.EventType,
      l.ProcedureRef,
      l.RequestPayload,
      l.ResponsePayload,
      l.HttpStatus,
      l.DurationMs,
      l.LoggedAt)).ToList();

    return Result<OtIntegrationLogsPageDto, OtError>.Success(
      new OtIntegrationLogsPageDto(data, total, effectivePage, effectivePageSize));
  }
}
