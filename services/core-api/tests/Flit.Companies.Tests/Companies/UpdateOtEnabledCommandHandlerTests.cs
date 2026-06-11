using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para UpdateOtEnabledCommandHandler.
/// AC3 HU-9776 — PUT /api/v1/admin/companies/{id}/config/ot-enabled
/// </summary>
public class UpdateOtEnabledCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly UpdateOtEnabledCommandHandler _sut;

    public UpdateOtEnabledCommandHandlerTests()
    {
        _sut = new UpdateOtEnabledCommandHandler(_companyRepo);
    }

    // ─── AC3: actualización exitosa ───────────────────────────────────────────

    [Fact]
    public async Task AC3_ActualizarOtEnabled_EntridasValidas_RetornaListaActualizada()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = BuildValidCommand(companyId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.OtSlug == "simit-bogota" && e.ProcedureFamily == "matricula_inicial");
        result.Value.Should().Contain(e => e.OtSlug == "runt-medellin" && e.ProcedureFamily == "traspasos");
    }

    [Fact]
    public async Task AC3_ActualizarOtEnabled_LlamaReplaceEnRepositorio()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        // Act
        await _sut.HandleAsync(BuildValidCommand(companyId));

        // Assert
        await _companyRepo.Received(1).ReplaceOtEnabledAsync(
            companyId,
            Arg.Any<IEnumerable<CompanyOtEnabled>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ActualizarOtEnabled_CompaniaNoExiste_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        // Act
        var result = await _sut.HandleAsync(BuildValidCommand(companyId));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_NOT_FOUND");
    }

    // ─── Validación procedure_family ──────────────────────────────────────────

    [Theory]
    [InlineData("MATRICULA_INICIAL")]
    [InlineData("todos")]
    [InlineData("")]
    public async Task AC3_ActualizarOtEnabled_ProcedureFamilyInvalida_RetornaError(string procedureFamily)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateOtEnabledCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new OtEnabledEntry("simit-bogota", procedureFamily)]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_PROCEDURE_FAMILY");
    }

    [Theory]
    [InlineData("matricula_inicial")]
    [InlineData("traspasos")]
    [InlineData("otros")]
    public async Task AC3_ActualizarOtEnabled_ProcedureFamilyValida_NoFalla(string procedureFamily)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateOtEnabledCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new OtEnabledEntry("simit-bogota", procedureFamily)]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AC3_ActualizarOtEnabled_OtSlugVacio_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateOtEnabledCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new OtEnabledEntry("   ", "matricula_inicial")]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_PROCEDURE_FAMILY");
    }

    [Fact]
    public async Task AC3_ActualizarOtEnabled_OtSlugNormalizadoAMinusculas()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateOtEnabledCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new OtEnabledEntry("SIMIT-BOGOTA", "matricula_inicial")]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].OtSlug.Should().Be("simit-bogota");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Company BuildCompany(Guid companyId) => new()
    {
        Id = companyId,
        TenantId = Guid.NewGuid(),
        Nit = "900111222-3",
        Name = "Test Company",
        Status = "active",
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = Guid.NewGuid()
    };

    private static UpdateOtEnabledCommand BuildValidCommand(Guid companyId) =>
        new(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries:
            [
                new OtEnabledEntry("simit-bogota", "matricula_inicial"),
                new OtEnabledEntry("runt-medellin", "traspasos", IsEnabled: true)
            ]);
}
