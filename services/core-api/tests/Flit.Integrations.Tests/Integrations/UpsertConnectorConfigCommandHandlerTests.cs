using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Application.Commands;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Integrations.Tests.Integrations;

/// <summary>
/// Tests unitarios para UpsertConnectorConfigCommandHandler.
/// AC1 HU-9775: el proveedor primario de RUNT es configurable por tenant.
/// </summary>
public class UpsertConnectorConfigCommandHandlerTests
{
    private readonly IConnectorConfigRepository _configRepo = Substitute.For<IConnectorConfigRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly UpsertConnectorConfigCommandHandler _sut;

    public UpsertConnectorConfigCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new UpsertConnectorConfigCommandHandler(_configRepo, _clock);
        _configRepo.GetActiveByTenantAndTypeAsync(Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig>());
    }

    // ─── AC1: configurar proveedor primario ───────────────────────────────────

    [Fact]
    public async Task AC1_UpsertNuevoConector_VerifikPrimario_RetornaDto()
    {
        // Arrange
        var command = new UpsertConnectorConfigCommand(
            TenantId, "runt", "verifik",
            IsPrimary: true, Priority: 1, TimeoutMs: 4000,
            IsActive: true, RequestedByUserId: UserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("verifik");
        result.Value.ConnectorType.Should().Be("runt");
        result.Value.IsPrimary.Should().BeTrue();
        result.Value.Priority.Should().Be(1);
        result.Value.TimeoutMs.Should().Be(4000);
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task AC1_UpsertNuevoConector_PersisteLlamadaUpsert()
    {
        // Arrange
        var command = new UpsertConnectorConfigCommand(
            TenantId, "runt", "intempo",
            IsPrimary: false, Priority: 2, TimeoutMs: 5000,
            IsActive: true, RequestedByUserId: UserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert: se llama Upsert una vez con el config correcto
        await _configRepo.Received(1).UpsertAsync(
            Arg.Is<ConnectorConfig>(c =>
                c.TenantId == TenantId
                && c.ConnectorType == "runt"
                && c.Provider == "intempo"
                && !c.IsPrimary
                && c.Priority == 2
                && c.TimeoutMs == 5000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_UpsertConectorExistente_ActualizaValores()
    {
        // Arrange: ya existe un config para verifik
        var existingConfig = new ConnectorConfig
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            ConnectorType = "runt",
            Provider = "verifik",
            IsPrimary = true,
            Priority = 1,
            TimeoutMs = 4000,
            IsActive = true,
            CredentialsRef = "{}",
            CreatedAt = FixedNow.AddDays(-1),
            CreatedBy = UserId,
            UpdatedAt = FixedNow.AddDays(-1),
            UpdatedBy = UserId,
            RowVersion = 1
        };

        _configRepo.GetActiveByTenantAndTypeAsync(TenantId, "runt", Arg.Any<CancellationToken>())
            .Returns(new List<ConnectorConfig> { existingConfig });

        var command = new UpsertConnectorConfigCommand(
            TenantId, "runt", "verifik",
            IsPrimary: true, Priority: 1, TimeoutMs: 6000,
            IsActive: true, RequestedByUserId: UserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert: actualiza el timeout
        result.IsSuccess.Should().BeTrue();
        result.Value.TimeoutMs.Should().Be(6000);
    }

    // ─── Validaciones ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("payco")]
    [InlineData("google")]
    [InlineData("")]
    public async Task AC1_ProveedorInvalido_RetornaError(string provider)
    {
        var command = new UpsertConnectorConfigCommand(
            TenantId, "runt", provider,
            IsPrimary: true, Priority: 1, TimeoutMs: 4000,
            IsActive: true, RequestedByUserId: UserId);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INTEGRATIONS_INVALID_PROVIDER");
    }

    [Theory]
    [InlineData("vehiculos")]
    [InlineData("crm")]
    [InlineData("")]
    public async Task AC1_TipoConectorInvalido_RetornaError(string connectorType)
    {
        var command = new UpsertConnectorConfigCommand(
            TenantId, connectorType, "verifik",
            IsPrimary: true, Priority: 1, TimeoutMs: 4000,
            IsActive: true, RequestedByUserId: UserId);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INTEGRATIONS_INVALID_CONNECTOR_TYPE");
    }

    [Theory]
    [InlineData("verifik")]
    [InlineData("intempo")]
    [InlineData("mock")]
    public async Task AC1_ProveedoresValidos_RetornanExito(string provider)
    {
        var command = new UpsertConnectorConfigCommand(
            TenantId, "runt", provider,
            IsPrimary: true, Priority: 1, TimeoutMs: 4000,
            IsActive: true, RequestedByUserId: UserId);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("runt")]
    [InlineData("simit")]
    [InlineData("rues")]
    [InlineData("identity")]
    [InlineData("quipux")]
    public async Task AC1_TiposConectorValidos_RetornanExito(string connectorType)
    {
        var command = new UpsertConnectorConfigCommand(
            TenantId, connectorType, "mock",
            IsPrimary: true, Priority: 1, TimeoutMs: 4000,
            IsActive: true, RequestedByUserId: UserId);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }
}
