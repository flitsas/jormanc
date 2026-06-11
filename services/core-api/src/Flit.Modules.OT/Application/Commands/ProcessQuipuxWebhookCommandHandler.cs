using System.Diagnostics;
using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

/// <summary>
/// AC1/AC2 HU-9800 — valida HMAC, actualiza estado del trámite y registra ot_integration_logs.
/// </summary>
public sealed class ProcessQuipuxWebhookCommandHandler(
  IOtOrganismRepository organismRepository,
  IOtIntegrationLogRepository logRepository,
  IQuipuxWebhookValidator webhookValidator,
  IProcedureStatusUpdater procedureStatusUpdater,
  IProcedureStatusNotifier procedureStatusNotifier,
  IClock clock)
{
  public async Task<Result<QuipuxWebhookResponseDto, OtError>> HandleAsync(
    ProcessQuipuxWebhookCommand command,
    CancellationToken ct = default)
  {
    var sw = Stopwatch.StartNew();
    var slug = command.OtSlug.Trim().ToLowerInvariant();

    var organism = await organismRepository.FindBySlugAsync(slug, ct);
    if (organism is null)
      return Result<QuipuxWebhookResponseDto, OtError>.Failure(OtError.SlugNotFound);

    if (!organism.QuipuxEnabled || organism.Mode != "qx")
    {
      await PersistLogAsync(
        organism,
        "quipux_not_enabled",
        null,
        command.RawBody,
        null,
        httpStatus: 400,
        durationMs: (int)sw.ElapsedMilliseconds,
        ct);
      return Result<QuipuxWebhookResponseDto, OtError>.Failure(OtError.QuipuxNotEnabled);
    }

    if (!webhookValidator.IsValid(
        organism.QuipuxConfig,
        command.RawBody,
        command.QuipuxToken,
        command.QuipuxSignature))
    {
      await PersistLogAsync(
        organism,
        "invalid_hmac_attempt",
        null,
        command.RawBody,
        null,
        httpStatus: 401,
        durationMs: (int)sw.ElapsedMilliseconds,
        ct);
      return Result<QuipuxWebhookResponseDto, OtError>.Failure(OtError.InvalidWebhookSignature);
    }

    if (!TryParsePayload(command.RawBody, out var eventType, out var procedureRef, out var newStatus))
    {
      await PersistLogAsync(
        organism,
        "invalid_payload",
        null,
        command.RawBody,
        "{\"error\":\"invalid_payload\"}",
        httpStatus: 400,
        durationMs: (int)sw.ElapsedMilliseconds,
        ct);
      return Result<QuipuxWebhookResponseDto, OtError>.Failure(
        new OtError("OT_INVALID_WEBHOOK_PAYLOAD", "Payload del webhook inválido."));
    }

    if (eventType == "status_changed"
        && !string.IsNullOrWhiteSpace(procedureRef)
        && !string.IsNullOrWhiteSpace(newStatus))
    {
      var procedureId = await procedureStatusUpdater.UpdateByCompositeIdAsync(
        organism.TenantId,
        procedureRef.Trim(),
        newStatus.Trim(),
        ct);

      if (procedureId is null)
      {
        await PersistLogAsync(
          organism,
          eventType,
          procedureRef,
          command.RawBody,
          "{\"error\":\"procedure_not_found\"}",
          httpStatus: 404,
          durationMs: (int)sw.ElapsedMilliseconds,
          ct);
        return Result<QuipuxWebhookResponseDto, OtError>.Failure(OtError.ProcedureNotFound);
      }

      await procedureStatusNotifier.NotifyStatusUpdateAsync(
        organism.TenantId,
        procedureId.Value,
        procedureRef.Trim(),
        newStatus.Trim(),
        ct);
    }

    var responseJson = "{\"processed\":true}";
    await PersistLogAsync(
      organism,
      eventType,
      procedureRef,
      command.RawBody,
      responseJson,
      httpStatus: 200,
      durationMs: (int)sw.ElapsedMilliseconds,
      ct);

    return Result<QuipuxWebhookResponseDto, OtError>.Success(
      new QuipuxWebhookResponseDto(eventType, procedureRef, newStatus, true));
  }

  private static bool TryParsePayload(
    string rawBody,
    out string eventType,
    out string? procedureRef,
    out string? newStatus)
  {
    eventType = string.Empty;
    procedureRef = null;
    newStatus = null;

    try
    {
      using var doc = JsonDocument.Parse(rawBody);
      var root = doc.RootElement;

      if (root.TryGetProperty("event", out var eventProp))
        eventType = eventProp.GetString() ?? string.Empty;
      else if (root.TryGetProperty("event_type", out var eventTypeProp))
        eventType = eventTypeProp.GetString() ?? string.Empty;

      if (root.TryGetProperty("procedure_ref", out var refProp))
        procedureRef = refProp.GetString();

      if (root.TryGetProperty("new_status", out var statusProp))
        newStatus = statusProp.GetString();

      return !string.IsNullOrWhiteSpace(eventType);
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private async Task PersistLogAsync(
    OtOrganism organism,
    string eventType,
    string? procedureRef,
    string requestPayload,
    string? responsePayload,
    int httpStatus,
    int durationMs,
    CancellationToken ct)
  {
    var log = new OtIntegrationLog
    {
      Id = Guid.NewGuid(),
      OtId = organism.Id,
      TenantId = organism.TenantId,
      EventType = eventType,
      ProcedureRef = procedureRef,
      RequestPayload = requestPayload,
      ResponsePayload = responsePayload,
      HttpStatus = httpStatus,
      DurationMs = durationMs,
      LoggedAt = clock.UtcNow
    };

    await logRepository.AddAsync(log, ct);
  }
}

public sealed record QuipuxWebhookResponseDto(
  string EventType,
  string? ProcedureRef,
  string? NewStatus,
  bool Processed);
