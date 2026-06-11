using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Queries;
using Flit.Modules.OT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.Quipux;

/// <summary>
/// AC3 HU-9800 — logs Quipux paginables e inmutables (solo lectura).
/// </summary>
public class GetOtIntegrationLogsQueryHandlerTests
{
  private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
  private readonly IOtIntegrationLogRepository _logRepo = Substitute.For<IOtIntegrationLogRepository>();

  private static readonly Guid TenantId = Guid.NewGuid();
  private static readonly Guid OtId = Guid.NewGuid();

  private readonly GetOtIntegrationLogsQueryHandler _sut;

  public GetOtIntegrationLogsQueryHandlerTests()
  {
    _sut = new GetOtIntegrationLogsQueryHandler(_organismRepo, _logRepo);
  }

  [Fact]
  public async Task AC3_ListaLogs_FiltraPorEventTypeYPagina()
  {
    var organism = new OtOrganism { Id = OtId, TenantId = TenantId, Slug = "ot-test", Name = "OT" };
    _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
      .Returns(organism);

    var loggedAt = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);
    var logs = new List<OtIntegrationLog>
    {
      new()
      {
        Id = Guid.NewGuid(),
        OtId = OtId,
        TenantId = TenantId,
        EventType = "status_changed",
        ProcedureRef = "TRASP-02_EVE-8841",
        HttpStatus = 200,
        DurationMs = 42,
        LoggedAt = loggedAt
      }
    };

    _logRepo.ListAsync(Arg.Any<OtIntegrationLogFilter>(), Arg.Any<CancellationToken>())
      .Returns((Items: (IReadOnlyList<OtIntegrationLog>)logs, Total: 500));

    var query = new GetOtIntegrationLogsQuery(
      OtId,
      TenantId,
      Page: 1,
      PageSize: 20,
      EventType: "status_changed");

    var result = await _sut.HandleAsync(query);

    result.IsSuccess.Should().BeTrue();
    result.Value.Data.Should().HaveCount(1);
    result.Value.Total.Should().Be(500);
    result.Value.Page.Should().Be(1);
    result.Value.PageSize.Should().Be(20);

    var item = result.Value.Data[0];
    item.EventType.Should().Be("status_changed");
    item.ProcedureRef.Should().Be("TRASP-02_EVE-8841");
    item.HttpStatus.Should().Be(200);
    item.DurationMs.Should().Be(42);
    item.LoggedAt.Should().Be(loggedAt);

    await _logRepo.Received(1).ListAsync(
      Arg.Is<OtIntegrationLogFilter>(f =>
        f.TenantId == TenantId &&
        f.OtId == OtId &&
        f.EventType == "status_changed" &&
        f.Page == 1 &&
        f.PageSize == 20),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AC3_PageSizeExcesivo_SeLimitaA100()
  {
    _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
      .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });

    _logRepo.ListAsync(Arg.Any<OtIntegrationLogFilter>(), Arg.Any<CancellationToken>())
      .Returns(((IReadOnlyList<OtIntegrationLog>)new List<OtIntegrationLog>(), 0));

    var query = new GetOtIntegrationLogsQuery(OtId, TenantId, 1, 999);
    await _sut.HandleAsync(query);

    await _logRepo.Received(1).ListAsync(
      Arg.Is<OtIntegrationLogFilter>(f => f.PageSize == 100),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AC3_OtNoEncontrado_Retorna404()
  {
    _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
      .Returns((OtOrganism?)null);

    var result = await _sut.HandleAsync(new GetOtIntegrationLogsQuery(OtId, TenantId));

    result.IsSuccess.Should().BeFalse();
    result.Error.Code.Should().Be("OT_NOT_FOUND");
  }
}
