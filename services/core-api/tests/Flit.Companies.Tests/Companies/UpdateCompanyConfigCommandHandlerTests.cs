using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para UpdateCompanyConfigCommandHandler.
/// AC3 HU-9774: PATCH /api/v1/admin/companies/{id}/config actualiza pestañas.
/// </summary>
public class UpdateCompanyConfigCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly UpdateCompanyConfigCommandHandler _sut;

    public UpdateCompanyConfigCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new UpdateCompanyConfigCommandHandler(_companyRepo, _clock);
    }

    // ─── AC3: actualización exitosa ───────────────────────────────────────────

    [Fact]
    public async Task AC3_ActualizarConfig_PestanasValidas_RetornaConfigDto()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = BuildCompanyWithConfig(companyId);
        _companyRepo.FindByIdWithConfigAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var command = BuildValidCommand(companyId, onlyOwnVehicles: true, notificationTarget: "comprador");

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OnlyOwnVehicles.Should().BeTrue();
        result.Value.NotificationTarget.Should().Be("comprador");
        result.Value.CompanyId.Should().Be(companyId);
    }

    [Fact]
    public async Task AC3_ActualizarConfig_PersistiCambiosEnRepositorio()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = BuildCompanyWithConfig(companyId);
        _companyRepo.FindByIdWithConfigAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var command = BuildValidCommand(companyId);

        // Act
        await _sut.HandleAsync(command);

        // Assert: persiste los cambios
        await _companyRepo.Received(1)
            .UpdateConfigAsync(Arg.Any<CompanyConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ActualizarConfig_UpdatedAtSeActualiza()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = BuildCompanyWithConfig(companyId);
        _companyRepo.FindByIdWithConfigAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        // Act
        await _sut.HandleAsync(BuildValidCommand(companyId));

        // Assert: updated_at debe ser el reloj del sistema
        await _companyRepo.Received(1).UpdateConfigAsync(
            Arg.Is<CompanyConfig>(cfg => cfg.UpdatedAt == FixedNow),
            Arg.Any<CancellationToken>());
    }

    // ─── Compañía no encontrada ───────────────────────────────────────────────

    [Fact]
    public async Task AC3_ActualizarConfig_CompaniaNoExiste_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdWithConfigAsync(companyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var command = BuildValidCommand(companyId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_NOT_FOUND");
    }

    // ─── Validaciones de dominio ──────────────────────────────────────────────

    [Theory]
    [InlineData("invalido")]
    [InlineData("")]
    [InlineData("COMPRADOR")]
    public async Task AC3_ActualizarConfig_NotificationTargetInvalido_RetornaError(string target)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = BuildValidCommand(companyId, notificationTarget: target);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_NOTIFICATION_TARGET");
    }

    [Theory]
    [InlineData("comprador")]
    [InlineData("radicador")]
    [InlineData("ninguno")]
    public async Task AC3_ActualizarConfig_NotificationTargetValido_NoFalla(string target)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = BuildCompanyWithConfig(companyId);
        _companyRepo.FindByIdWithConfigAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var command = BuildValidCommand(companyId, notificationTarget: target);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("smtp_externo")]
    [InlineData("NATIVE")]
    public async Task AC3_ActualizarConfig_SmtpModeInvalido_RetornaError(string smtpMode)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var command = BuildValidCommand(companyId, smtpMode: smtpMode);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_SMTP_MODE");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Company BuildCompanyWithConfig(Guid companyId)
    {
        var config = new CompanyConfig
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OnlyOwnVehicles = false,
            BaulFirmasEnabled = false,
            NotificationTarget = "radicador",
            SmtpMode = "native",
            MatriculaConfig = "{}",
            TraspasosConfig = "{}",
            ContingencyConfig = "{}",
            RecaudoMethods = "[]",
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var company = new Company
        {
            Id = companyId,
            TenantId = Guid.NewGuid(),
            Nit = "900111222-3",
            Name = "Test Company",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedBy = Guid.NewGuid()
        };

        company.Config = config;
        config.Company = company;

        return company;
    }

    private static UpdateCompanyConfigCommand BuildValidCommand(
        Guid companyId,
        bool onlyOwnVehicles = false,
        bool baulFirmasEnabled = false,
        string notificationTarget = "radicador",
        string smtpMode = "native") =>
        new(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            OnlyOwnVehicles: onlyOwnVehicles,
            BaulFirmasEnabled: baulFirmasEnabled,
            NotificationTarget: notificationTarget,
            SmtpMode: smtpMode,
            MatriculaConfig: "{\"matricula_enabled\": true}",
            TraspasosConfig: "{\"traspasos_enabled\": false}",
            ContingencyConfig: "{}",
            RecaudoMethods: "[\"efectivo\"]");
}
