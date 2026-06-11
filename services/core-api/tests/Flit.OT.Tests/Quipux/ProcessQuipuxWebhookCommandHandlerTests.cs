using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.Modules.OT.Infrastructure.Webhooks;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.Quipux;

/// <summary>
/// AC1/AC2 HU-9800 — webhook Quipux con validación HMAC y registro en ot_integration_logs.
/// </summary>
public class ProcessQuipuxWebhookCommandHandlerTests
{
  private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
  private readonly IOtIntegrationLogRepository _logRepo = Substitute.For<IOtIntegrationLogRepository>();
  private readonly IProcedureStatusUpdater _statusUpdater = Substitute.For<IProcedureStatusUpdater>();
  private readonly IProcedureStatusNotifier _statusNotifier = Substitute.For<IProcedureStatusNotifier>();
  private readonly IClock _clock = Substitute.For<IClock>();

  private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 0, 0, TimeSpan.Zero);
  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid OtId = Guid.NewGuid();
  private static readonly Guid ProcedureId = Guid.NewGuid();

  private readonly ProcessQuipuxWebhookCommandHandler _sut;

  public ProcessQuipuxWebhookCommandHandlerTests()
  {
    _clock.UtcNow.Returns(FixedNow);
    _sut = new ProcessQuipuxWebhookCommandHandler(
      _organismRepo,
      _logRepo,
      new QuipuxWebhookValidator(),
      _statusUpdater,
      _statusNotifier,
      _clock);
  }

  [Fact]
  public async Task AC1_HmacValido_ActualizaEstado_RegistraLogYNotificaSignalR()
  {
    const string compositeId = "TRASP-02_EVE-8841";
    const string rawBody =
      "{\"event\":\"status_changed\",\"procedure_ref\":\"TRASP-02_EVE-8841\",\"new_status\":\"approved\"}";
    var token = QuipuxWebhookTestHelpers.DefaultToken;
    var signature = QuipuxWebhookTestHelpers.ComputeSignature(rawBody, token);

    var organism = BuildOrganism();
    _organismRepo.FindBySlugAsync("secretaria-bogota", Arg.Any<CancellationToken>())
      .Returns(organism);
    _statusUpdater.UpdateByCompositeIdAsync(TenantId, compositeId, "approved", Arg.Any<CancellationToken>())
      .Returns(ProcedureId);

    var command = new ProcessQuipuxWebhookCommand(
      "secretaria-bogota",
      rawBody,
      token,
      signature);

    var result = await _sut.HandleAsync(command);

    result.IsSuccess.Should().BeTrue();
    result.Value.EventType.Should().Be("status_changed");
    result.Value.Processed.Should().BeTrue();

    await _statusUpdater.Received(1).UpdateByCompositeIdAsync(
      TenantId,
      compositeId,
      "approved",
      Arg.Any<CancellationToken>());

    await _statusNotifier.Received(1).NotifyStatusUpdateAsync(
      TenantId,
      ProcedureId,
      compositeId,
      "approved",
      Arg.Any<CancellationToken>());

    await _logRepo.Received(1).AddAsync(
      Arg.Is<OtIntegrationLog>(l =>
        l.OtId == OtId &&
        l.EventType == "status_changed" &&
        l.ProcedureRef == compositeId &&
        l.HttpStatus == 200 &&
        l.DurationMs.HasValue),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AC2_HmacInvalido_RetornaError_RegistraIntentoSinActualizarEstado()
  {
    const string rawBody =
      "{\"event\":\"status_changed\",\"procedure_ref\":\"TRASP-02_EVE-8841\",\"new_status\":\"approved\"}";

    var organism = BuildOrganism();
    _organismRepo.FindBySlugAsync("secretaria-bogota", Arg.Any<CancellationToken>())
      .Returns(organism);

    var command = new ProcessQuipuxWebhookCommand(
      "secretaria-bogota",
      rawBody,
      QuipuxWebhookTestHelpers.DefaultToken,
      "invalid-signature");

    var result = await _sut.HandleAsync(command);

    result.IsSuccess.Should().BeFalse();
    result.Error.Code.Should().Be("OT_INVALID_WEBHOOK_SIGNATURE");

    await _statusUpdater.DidNotReceive().UpdateByCompositeIdAsync(
      Arg.Any<Guid>(),
      Arg.Any<string>(),
      Arg.Any<string>(),
      Arg.Any<CancellationToken>());

    await _logRepo.Received(1).AddAsync(
      Arg.Is<OtIntegrationLog>(l =>
        l.EventType == "invalid_hmac_attempt" &&
        l.HttpStatus == 401),
      Arg.Any<CancellationToken>());
  }

  private static OtOrganism BuildOrganism() => new()
  {
    Id = OtId,
    TenantId = TenantId,
    Slug = "secretaria-bogota",
    Name = "Secretaría de Bogotá",
    Mode = "qx",
    QuipuxEnabled = true,
    QuipuxConfig = QuipuxWebhookTestHelpers.BuildQuipuxConfig()
  };
}
