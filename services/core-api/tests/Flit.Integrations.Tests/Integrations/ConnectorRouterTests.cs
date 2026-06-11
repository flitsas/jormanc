using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Domain.Models;
using Flit.Modules.Integrations.Infrastructure;
using Flit.Modules.Integrations.Infrastructure.Connectors.Runt;
using Flit.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Flit.Integrations.Tests.Integrations;

/// <summary>
/// Tests unitarios para ConnectorRouter (AC1, AC2, AC3 HU-9775).
/// AC1: la consulta usa el proveedor primario configurado en connector_configs.
/// AC2: timeout >4s o HTTP 5xx → failover automático al secundario.
/// AC3: cada llamada genera IntegrationLog por tenant.
/// </summary>
public class ConnectorRouterTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly IConnectorConfigRepository _configRepo = Substitute.For<IConnectorConfigRepository>();
    private readonly IIntegrationLogRepository _logRepo = Substitute.For<IIntegrationLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly MockRuntConnector _mock = new();
    private readonly VerifikRuntConnector _verifik;
    private readonly IntempoRuntConnector _intempo;

    public ConnectorRouterTests()
    {
        _clock.UtcNow.Returns(FixedNow);

        var verifikOpts = Options.Create(new VerifikOptions { ApiKey = "mock" });
        var intempoOpts = Options.Create(new IntempoOptions { ApiKey = "mock" });
        var httpFactory = Substitute.For<IHttpClientFactory>();

        _verifik = new VerifikRuntConnector(httpFactory, verifikOpts,
            NullLogger<VerifikRuntConnector>.Instance);
        _intempo = new IntempoRuntConnector(httpFactory, intempoOpts,
            NullLogger<IntempoRuntConnector>.Instance);
    }

    private ConnectorRouter BuildSut() =>
        new(new IRuntConnector[] { _verifik, _intempo, _mock },
            _configRepo, _logRepo, _clock,
            NullLogger<ConnectorRouter>.Instance);

    // ─── AC1: proveedor primario configurado en connector_configs ─────────────

    [Fact]
    public async Task AC1_ConsultaPlaca_UsaProvedorPrimario_VerifikPrimario()
    {
        // Arrange: verifik es primario (priority=1)
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 4000)
            });

        var sut = BuildSut();

        // Act
        var (result, provider, _) = await sut.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("AAA123", ct),
            "runt.vehicle.plate");

        // Assert
        provider.Should().Be("verifik");
        result.Found.Should().BeTrue();
        result.Plate.Should().Be("AAA123");
    }

    [Fact]
    public async Task AC1_ConsultaPlaca_IntempoPrimario_UsaIntempo()
    {
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("intempo", isPrimary: true, priority: 1, timeoutMs: 4000)
            });

        var sut = BuildSut();

        var (result, provider, _) = await sut.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("XYZ789", ct),
            "runt.vehicle.plate");

        provider.Should().Be("intempo");
        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task AC1_SinConfiguracion_UsaMockPorDefecto()
    {
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>());

        var sut = BuildSut();

        var (result, provider, _) = await sut.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("DEF456", ct),
            "runt.vehicle.plate");

        provider.Should().Be("mock");
        result.Found.Should().BeTrue();
    }

    // ─── AC2: failover automático al secundario en timeout o 5xx ─────────────

    [Fact]
    public async Task AC2_PrimarioTimeout_FailoverAlSecundario()
    {
        // Verifik primario con timeout de 50ms (suficiente para que SlowVerifikConnector tarde más)
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 50),
                BuildConfig("intempo", isPrimary: false, priority: 2, timeoutMs: 4000)
            });

        var router = new ConnectorRouter(
            runtConnectors: new IRuntConnector[] { new SlowVerifikConnector(), _intempo, _mock },
            configRepository: _configRepo,
            logRepository: _logRepo,
            clock: _clock,
            logger: NullLogger<ConnectorRouter>.Instance);

        // Act
        var (result, provider, _) = await router.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("GHI012", ct),
            "runt.vehicle.plate");

        // Assert: failover a intempo (mock mode)
        provider.Should().Be("intempo");
        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task AC2_Primario5xx_FailoverAlSecundario()
    {
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 4000),
                BuildConfig("intempo", isPrimary: false, priority: 2, timeoutMs: 4000)
            });

        var router = new ConnectorRouter(
            runtConnectors: new IRuntConnector[] { new ServerErrorVerifikConnector(), _intempo, _mock },
            configRepository: _configRepo,
            logRepository: _logRepo,
            clock: _clock,
            logger: NullLogger<ConnectorRouter>.Instance);

        var (result, provider, _) = await router.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("JKL345", ct),
            "runt.vehicle.plate");

        provider.Should().Be("intempo");
        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task AC2_TodosLosConectoresFallan_LanzaAllConnectorsFailedException()
    {
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 4000),
                BuildConfig("intempo", isPrimary: false, priority: 2, timeoutMs: 4000)
            });

        var router = new ConnectorRouter(
            runtConnectors: new IRuntConnector[] { new ServerErrorVerifikConnector(), new ServerErrorIntempoConnector(), _mock },
            configRepository: _configRepo,
            logRepository: _logRepo,
            clock: _clock,
            logger: NullLogger<ConnectorRouter>.Instance);

        Func<Task> act = async () => await router.ExecuteRuntAsync<VehicleQueryResult>(
            TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("MNO678", ct),
            "runt.vehicle.plate");

        await act.Should().ThrowAsync<AllConnectorsFailedException>()
            .WithMessage("*runt.vehicle.plate*");
    }

    // ─── AC3: cada llamada genera IntegrationLog por tenant ──────────────────

    [Fact]
    public async Task AC3_ConsultaExitosa_PersistLogConTenantId()
    {
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 4000)
            });

        IntegrationLog? capturedLog = null;
        await _logRepo.AddAsync(Arg.Do<IntegrationLog>(l => capturedLog = l),
            Arg.Any<CancellationToken>());

        var sut = BuildSut();

        await sut.ExecuteRuntAsync<VehicleQueryResult>(TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("PQR901", ct),
            "runt.vehicle.plate");

        await _logRepo.Received(1).AddAsync(Arg.Any<IntegrationLog>(), Arg.Any<CancellationToken>());
        capturedLog.Should().NotBeNull();
        capturedLog!.TenantId.Should().Be(TenantId);
        capturedLog.ConnectorType.Should().Be("runt");
        capturedLog.Operation.Should().Be("runt.vehicle.plate");
        capturedLog.Provider.Should().Be("verifik");
        capturedLog.HttpStatus.Should().Be(200);
        capturedLog.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AC3_FailoverConTimeout_RegistraDosLogs_TimeoutYSuccess()
    {
        // verifik timeout (50ms), intempo exitoso
        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("verifik", isPrimary: true, priority: 1, timeoutMs: 50),
                BuildConfig("intempo", isPrimary: false, priority: 2, timeoutMs: 4000)
            });

        var capturedLogs = new List<IntegrationLog>();
        await _logRepo.AddAsync(Arg.Do<IntegrationLog>(l => capturedLogs.Add(l)),
            Arg.Any<CancellationToken>());

        var router = new ConnectorRouter(
            runtConnectors: new IRuntConnector[] { new SlowVerifikConnector(), _intempo, _mock },
            configRepository: _configRepo,
            logRepository: _logRepo,
            clock: _clock,
            logger: NullLogger<ConnectorRouter>.Instance);

        await router.ExecuteRuntAsync<VehicleQueryResult>(TenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("STU234", ct),
            "runt.vehicle.plate");

        // Assert: dos logs (timeout verifik + success intempo)
        await _logRepo.Received(2).AddAsync(Arg.Any<IntegrationLog>(), Arg.Any<CancellationToken>());
        capturedLogs.Should().HaveCount(2);

        var timeoutLog = capturedLogs.First(l => l.Provider == "verifik");
        timeoutLog.HttpStatus.Should().Be(408);
        timeoutLog.ErrorMessage.Should().Contain("Timeout");

        var successLog = capturedLogs.First(l => l.Provider == "intempo");
        successLog.HttpStatus.Should().Be(200);
    }

    [Fact]
    public async Task AC3_LogContieneTenantIdYOperacion()
    {
        var expectedTenantId = Guid.NewGuid();
        _configRepo.GetActiveByTenantAndTypeAsync(expectedTenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>
            {
                BuildConfig("mock", isPrimary: true, priority: 1, timeoutMs: 4000, tenantId: expectedTenantId)
            });

        IntegrationLog? capturedLog = null;
        await _logRepo.AddAsync(Arg.Do<IntegrationLog>(l => capturedLog = l),
            Arg.Any<CancellationToken>());

        var sut = BuildSut();

        await sut.ExecuteRuntAsync<VehicleQueryResult>(expectedTenantId,
            (c, ct) => c.QueryVehicleByPlateAsync("VWX567", ct),
            "runt.vehicle.plate");

        capturedLog!.TenantId.Should().Be(expectedTenantId);
        capturedLog.Operation.Should().Be("runt.vehicle.plate");
        capturedLog.LoggedAt.Should().Be(FixedNow);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ConnectorConfig BuildConfig(
        string provider, bool isPrimary, int priority, int timeoutMs,
        Guid? tenantId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? TenantId,
            ConnectorType = "runt",
            Provider = provider,
            IsPrimary = isPrimary,
            Priority = priority,
            TimeoutMs = timeoutMs,
            IsActive = true,
            CredentialsRef = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
}

// ─── Stubs de conectores para escenarios de fallo ────────────────────────────

/// <summary>Conector que tarda 10s (garantiza timeout en tests).</summary>
internal sealed class SlowVerifikConnector : IRuntConnector
{
    public string ProviderName => "verifik";

    public async Task<VehicleQueryResult> QueryVehicleByPlateAsync(
        string plate, CancellationToken ct = default)
    {
        await Task.Delay(10_000, ct);
        return new VehicleQueryResult(false, null, null, null, null, null,
            null, null, null, null, null, Array.Empty<string>(), null);
    }

    public Task<VehicleQueryResult> QueryVehicleByVinAsync(string vin, CancellationToken ct = default) =>
        QueryVehicleByPlateAsync(vin, ct);

    public async Task<PersonQueryResult> QueryPersonAsync(string documentNumber, CancellationToken ct = default)
    {
        await Task.Delay(10_000, ct);
        return new PersonQueryResult(false, null, null, null, null, null, Array.Empty<string>(), null);
    }

    public async Task<RestrictionQueryResult> QueryRestrictionsAsync(string documentNumber, CancellationToken ct = default)
    {
        await Task.Delay(10_000, ct);
        return new RestrictionQueryResult(false, Array.Empty<RestrictionItem>(), null);
    }
}

/// <summary>Conector Verifik que siempre lanza ConnectorServerException (5xx).</summary>
internal sealed class ServerErrorVerifikConnector : IRuntConnector
{
    public string ProviderName => "verifik";

    public Task<VehicleQueryResult> QueryVehicleByPlateAsync(string plate, CancellationToken ct = default) =>
        throw new ConnectorServerException("verifik", 500, "runt.vehicle.plate");

    public Task<VehicleQueryResult> QueryVehicleByVinAsync(string vin, CancellationToken ct = default) =>
        throw new ConnectorServerException("verifik", 500, "runt.vehicle.vin");

    public Task<PersonQueryResult> QueryPersonAsync(string documentNumber, CancellationToken ct = default) =>
        throw new ConnectorServerException("verifik", 500, "runt.person");

    public Task<RestrictionQueryResult> QueryRestrictionsAsync(string documentNumber, CancellationToken ct = default) =>
        throw new ConnectorServerException("verifik", 500, "runt.restrictions");
}

/// <summary>Conector Intempo que siempre lanza ConnectorServerException (5xx).</summary>
internal sealed class ServerErrorIntempoConnector : IRuntConnector
{
    public string ProviderName => "intempo";

    public Task<VehicleQueryResult> QueryVehicleByPlateAsync(string plate, CancellationToken ct = default) =>
        throw new ConnectorServerException("intempo", 503, "runt.vehicle.plate");

    public Task<VehicleQueryResult> QueryVehicleByVinAsync(string vin, CancellationToken ct = default) =>
        throw new ConnectorServerException("intempo", 503, "runt.vehicle.vin");

    public Task<PersonQueryResult> QueryPersonAsync(string documentNumber, CancellationToken ct = default) =>
        throw new ConnectorServerException("intempo", 503, "runt.person");

    public Task<RestrictionQueryResult> QueryRestrictionsAsync(string documentNumber, CancellationToken ct = default) =>
        throw new ConnectorServerException("intempo", 503, "runt.restrictions");
}
