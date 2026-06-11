using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Application.Queries;
using Flit.Modules.Integrations.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Integrations.Tests.Integrations;

/// <summary>
/// Tests unitarios para GetIntegrationLogsQueryHandler.
/// AC3 HU-9775: cada llamada registra payload en integration_logs por tenant.
/// Verifica paginación, filtrado y estructura del resultado.
/// </summary>
public class GetIntegrationLogsQueryHandlerTests
{
    private readonly IIntegrationLogRepository _logRepo = Substitute.For<IIntegrationLogRepository>();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly GetIntegrationLogsQueryHandler _sut;

    public GetIntegrationLogsQueryHandlerTests()
    {
        _sut = new GetIntegrationLogsQueryHandler(_logRepo);
    }

    private static IntegrationLog BuildLog(
        string provider, string operation, int httpStatus = 200, int durationMs = 150) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            ConnectorType = "runt",
            Operation = operation,
            Provider = provider,
            RequestPayload = "{\"plate\":\"AAA123\"}",
            ResponsePayload = "{\"found\":true}",
            HttpStatus = httpStatus,
            DurationMs = durationMs,
            ErrorMessage = httpStatus >= 400 ? "Error de servidor" : null,
            LoggedAt = FixedNow
        };

    // ─── AC3: logs registrados por tenant ─────────────────────────────────────

    [Fact]
    public async Task AC3_ListarLogs_RetornaLogsPaginados()
    {
        // Arrange
        var logs = new List<IntegrationLog>
        {
            BuildLog("verifik", "runt.vehicle.plate"),
            BuildLog("intempo", "runt.vehicle.plate")
        };

        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns((Items: (IReadOnlyList<IntegrationLog>)logs, Total: 2));

        var query = new GetIntegrationLogsQuery(TenantId, Page: 1, PageSize: 20,
            ConnectorType: null, Provider: null, From: null, To: null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Data.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task AC3_ListarLogs_FiltroTenantIdPasadoAlRepositorio()
    {
        // Arrange
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog>(), 0));

        var query = new GetIntegrationLogsQuery(TenantId, Page: 1, PageSize: 20,
            ConnectorType: null, Provider: null, From: null, To: null);

        // Act
        await _sut.HandleAsync(query);

        // Assert: tenant_id se pasa correctamente al repositorio
        await _logRepo.Received(1).ListAsync(
            Arg.Is<IntegrationLogFilter>(f => f.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_LogContiene_Provider_Operation_TenantId()
    {
        // Arrange
        var log = BuildLog("verifik", "runt.vehicle.plate", 200, 120);
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog> { log }, 1));

        var query = new GetIntegrationLogsQuery(TenantId, 1, 20, null, null, null, null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert: estructura del DTO
        var dto = result.Data.First();
        dto.TenantId.Should().Be(TenantId);
        dto.Provider.Should().Be("verifik");
        dto.Operation.Should().Be("runt.vehicle.plate");
        dto.ConnectorType.Should().Be("runt");
        dto.HttpStatus.Should().Be(200);
        dto.DurationMs.Should().Be(120);
    }

    [Fact]
    public async Task AC3_FiltroConectorType_PasaCorrectamenteAlRepositorio()
    {
        // Arrange
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog>(), 0));

        var query = new GetIntegrationLogsQuery(TenantId, 1, 10, "runt", "verifik", null, null);

        // Act
        await _sut.HandleAsync(query);

        // Assert: filtros se pasan correctamente
        await _logRepo.Received(1).ListAsync(
            Arg.Is<IntegrationLogFilter>(f =>
                f.ConnectorType == "runt"
                && f.Provider == "verifik"
                && f.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_PageSizeMaximoCap_100()
    {
        // Arrange
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog>(), 0));

        // PageSize > 100 debe caparse a 100
        var query = new GetIntegrationLogsQuery(TenantId, 1, 999, null, null, null, null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        await _logRepo.Received(1).ListAsync(
            Arg.Is<IntegrationLogFilter>(f => f.PageSize == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_PageMenorA1_UsaPage1()
    {
        // Arrange
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog>(), 0));

        var query = new GetIntegrationLogsQuery(TenantId, 0, 20, null, null, null, null);

        // Act
        await _sut.HandleAsync(query);

        // Assert
        await _logRepo.Received(1).ListAsync(
            Arg.Is<IntegrationLogFilter>(f => f.Page == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_LogConTimeout_ContieneHttpStatus408()
    {
        // Arrange: log de timeout (HTTP 408)
        var log = BuildLog("verifik", "runt.vehicle.plate", httpStatus: 408, durationMs: 4001);
        log.ErrorMessage = "Timeout tras 4000ms";
        _logRepo.ListAsync(Arg.Any<IntegrationLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<IntegrationLog>)new List<IntegrationLog> { log }, 1));

        var query = new GetIntegrationLogsQuery(TenantId, 1, 20, null, null, null, null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert: el DTO expone el error de timeout
        var dto = result.Data.First();
        dto.HttpStatus.Should().Be(408);
        dto.ErrorMessage.Should().Contain("Timeout");
    }
}
