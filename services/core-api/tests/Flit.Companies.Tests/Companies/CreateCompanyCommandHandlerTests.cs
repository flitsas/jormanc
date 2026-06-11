using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para CreateCompanyCommandHandler.
/// AC1 HU-9774: POST /api/v1/admin/companies crea compañía + tenant + config por defecto.
/// </summary>
public class CreateCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly ITenantService _tenantService = Substitute.For<ITenantService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SystemUserId = Guid.NewGuid();

    private readonly CreateCompanyCommandHandler _sut;

    public CreateCompanyCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateCompanyCommandHandler(_companyRepo, _tenantService, _clock);
    }

    // ─── AC1: creación exitosa ────────────────────────────────────────────────

    [Fact]
    public async Task AC1_CrearCompania_NitNuevo_RetornaCompanyDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _companyRepo.NitExistsAsync("900111222-3", Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.SlugExistsAsync("acme-co", Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.CreateTenantAsync("acme-co", "Acme Co", Arg.Any<CancellationToken>()).Returns(tenantId);

        var command = new CreateCompanyCommand("900111222-3", "Acme Co", "acme-co", SystemUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nit.Should().Be("900111222-3");
        result.Value.Name.Should().Be("Acme Co");
        result.Value.TenantSlug.Should().Be("acme-co");
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.Status.Should().Be("active");
    }

    [Fact]
    public async Task AC1_CrearCompania_PersisteTenantYCompania()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _companyRepo.NitExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.CreateTenantAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(tenantId);

        var command = new CreateCompanyCommand("800200300-1", "Beta Corp", "beta-corp", SystemUserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert: debe crear tenant
        await _tenantService.Received(1)
            .CreateTenantAsync("beta-corp", "Beta Corp", Arg.Any<CancellationToken>());

        // Assert: debe persistir company + config
        await _companyRepo.Received(1).CreateAsync(
            Arg.Is<Company>(c => c.Nit == "800200300-1" && c.TenantId == tenantId),
            Arg.Is<CompanyConfig>(cfg => cfg.NotificationTarget == "radicador" && cfg.SmtpMode == "native"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CrearCompania_ConfigPorDefectoCorrectaEnCreacion()
    {
        // Arrange
        _companyRepo.NitExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.CreateTenantAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var command = new CreateCompanyCommand("700100200-5", "Gamma SA", "gamma-sa", SystemUserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert: config por defecto
        await _companyRepo.Received(1).CreateAsync(
            Arg.Any<Company>(),
            Arg.Is<CompanyConfig>(cfg =>
                !cfg.OnlyOwnVehicles &&
                !cfg.BaulFirmasEnabled &&
                cfg.NotificationTarget == "radicador" &&
                cfg.SmtpMode == "native" &&
                cfg.MatriculaConfig == "{}" &&
                cfg.TraspasosConfig == "{}" &&
                cfg.ContingencyConfig == "{}" &&
                cfg.RecaudoMethods == "[]"),
            Arg.Any<CancellationToken>());
    }

    // ─── NIT duplicado ────────────────────────────────────────────────────────

    [Fact]
    public async Task AC1_CrearCompania_NitDuplicado_RetornaError()
    {
        // Arrange
        _companyRepo.NitExistsAsync("900111222-3", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateCompanyCommand("900111222-3", "Duplicada SA", "dup-sa", SystemUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_NIT_ALREADY_EXISTS");
    }

    [Fact]
    public async Task AC1_CrearCompania_NitDuplicado_NoCreaATenant()
    {
        // Arrange
        _companyRepo.NitExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateCompanyCommand("900111222-3", "Dup SA", "dup-slug", SystemUserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert: no debe crear tenant
        await _tenantService.DidNotReceive()
            .CreateTenantAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── Tenant slug duplicado ────────────────────────────────────────────────

    [Fact]
    public async Task AC1_CrearCompania_TenantSlugDuplicado_RetornaError()
    {
        // Arrange
        _companyRepo.NitExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.SlugExistsAsync("existing-slug", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateCompanyCommand("900999888-7", "Nueva SA", "existing-slug", SystemUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_TENANT_SLUG_ALREADY_EXISTS");
    }

    [Fact]
    public async Task AC1_CrearCompania_TenantSlugDuplicado_NoPersiste()
    {
        // Arrange
        _companyRepo.NitExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _tenantService.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateCompanyCommand("900999888-7", "Nueva SA", "dup-slug", SystemUserId);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _companyRepo.DidNotReceive()
            .CreateAsync(Arg.Any<Company>(), Arg.Any<CompanyConfig>(), Arg.Any<CancellationToken>());
    }
}
