using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para UpdateSignatureMatrixCommandHandler.
/// AC1 HU-9776 — PUT /api/v1/admin/companies/{id}/config/signature-matrix
/// </summary>
public class UpdateSignatureMatrixCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly UpdateSignatureMatrixCommandHandler _sut;

    public UpdateSignatureMatrixCommandHandlerTests()
    {
        _sut = new UpdateSignatureMatrixCommandHandler(_companyRepo);
    }

    // ─── AC1: reemplazo exitoso ───────────────────────────────────────────────

    [Fact]
    public async Task AC1_ActualizarMatriz_EntridasValidas_RetornaMatrizActualizada()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var company = BuildCompany(companyId);
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var command = BuildValidCommand(companyId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.ActorRole == "vendedor" && e.SignatureType == "identidad_digital");
        result.Value.Should().Contain(e => e.ActorRole == "comprador" && e.SignatureType == "firma_pantalla");
    }

    [Fact]
    public async Task AC1_ActualizarMatriz_LlamaReplaceEnRepositorio()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        // Act
        await _sut.HandleAsync(BuildValidCommand(companyId));

        // Assert
        await _companyRepo.Received(1).ReplaceSignatureMatrixAsync(
            companyId,
            Arg.Any<IEnumerable<CompanySignatureMatrix>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_ActualizarMatriz_CompaniaNoExiste_RetornaError()
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

    // ─── Validación actor_role ────────────────────────────────────────────────

    [Theory]
    [InlineData("VENDEDOR")]
    [InlineData("cliente")]
    [InlineData("")]
    public async Task AC1_ActualizarMatriz_ActorRoleInvalido_RetornaError(string actorRole)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateSignatureMatrixCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new SignatureMatrixEntry(actorRole, "identidad_digital")]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_ACTOR_ROLE");
    }

    [Theory]
    [InlineData("vendedor")]
    [InlineData("comprador")]
    [InlineData("representante_legal")]
    public async Task AC1_ActualizarMatriz_ActorRoleValido_NoFalla(string actorRole)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateSignatureMatrixCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new SignatureMatrixEntry(actorRole, "preasignada")]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ─── Validación signature_type ────────────────────────────────────────────

    [Theory]
    [InlineData("IDENTIDAD_DIGITAL")]
    [InlineData("digital")]
    [InlineData("")]
    public async Task AC1_ActualizarMatriz_SignatureTypeInvalido_RetornaError(string signatureType)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateSignatureMatrixCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new SignatureMatrixEntry("vendedor", signatureType)]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_INVALID_SIGNATURE_TYPE");
    }

    [Theory]
    [InlineData("identidad_digital")]
    [InlineData("firma_pantalla")]
    [InlineData("preasignada")]
    public async Task AC1_ActualizarMatriz_SignatureTypeValido_NoFalla(string signatureType)
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));

        var command = new UpdateSignatureMatrixCommand(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries: [new SignatureMatrixEntry("vendedor", signatureType)]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
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

    private static UpdateSignatureMatrixCommand BuildValidCommand(Guid companyId) =>
        new(
            CompanyId: companyId,
            RequestedByUserId: AdminUserId,
            Entries:
            [
                new SignatureMatrixEntry("vendedor", "identidad_digital"),
                new SignatureMatrixEntry("comprador", "firma_pantalla")
            ]);
}
