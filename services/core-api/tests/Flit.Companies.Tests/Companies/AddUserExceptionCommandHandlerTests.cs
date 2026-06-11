using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para AddUserExceptionCommandHandler y RemoveUserExceptionCommandHandler.
/// AC2 HU-9776 — POST/DELETE /api/v1/admin/companies/{id}/user-exceptions
/// </summary>
public class UserExceptionCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly AddUserExceptionCommandHandler _addSut;
    private readonly RemoveUserExceptionCommandHandler _removeSut;

    public UserExceptionCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _addSut = new AddUserExceptionCommandHandler(_companyRepo, _clock);
        _removeSut = new RemoveUserExceptionCommandHandler(_companyRepo);
    }

    // ─── Add: éxito ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AC2_AgregarExcepcion_UsuarioNuevo_RetornaExcepcion()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));
        _companyRepo.UserExceptionExistsAsync(companyId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new AddUserExceptionCommand(companyId, targetUserId, AdminUserId);

        // Act
        var result = await _addSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyId.Should().Be(companyId);
        result.Value.UserId.Should().Be(targetUserId);
        result.Value.AddedBy.Should().Be(AdminUserId);
        result.Value.AddedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task AC2_AgregarExcepcion_LlamaPersistencia()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));
        _companyRepo.UserExceptionExistsAsync(companyId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _addSut.HandleAsync(new AddUserExceptionCommand(companyId, targetUserId, AdminUserId));

        // Assert
        await _companyRepo.Received(1)
            .AddUserExceptionAsync(Arg.Any<TenantUserException>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_AgregarExcepcion_CompaniaNoExiste_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var command = new AddUserExceptionCommand(companyId, Guid.NewGuid(), AdminUserId);

        // Act
        var result = await _addSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_NOT_FOUND");
    }

    [Fact]
    public async Task AC2_AgregarExcepcion_UsuarioYaEnListaBlanca_RetornaConflicto()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));
        _companyRepo.UserExceptionExistsAsync(companyId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new AddUserExceptionCommand(companyId, targetUserId, AdminUserId);

        // Act
        var result = await _addSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_USER_EXCEPTION_ALREADY_EXISTS");
    }

    // ─── Remove: éxito ────────────────────────────────────────────────────────

    [Fact]
    public async Task AC2_EliminarExcepcion_UsuarioEnListaBlanca_RetornaTrue()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));
        _companyRepo.RemoveUserExceptionAsync(companyId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new RemoveUserExceptionCommand(companyId, targetUserId, AdminUserId);

        // Act
        var result = await _removeSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AC2_EliminarExcepcion_CompaniaNoExiste_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var command = new RemoveUserExceptionCommand(companyId, Guid.NewGuid(), AdminUserId);

        // Act
        var result = await _removeSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_NOT_FOUND");
    }

    [Fact]
    public async Task AC2_EliminarExcepcion_UsuarioNoEnListaBlanca_RetornaError()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _companyRepo.FindByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(BuildCompany(companyId));
        _companyRepo.RemoveUserExceptionAsync(companyId, targetUserId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new RemoveUserExceptionCommand(companyId, targetUserId, AdminUserId);

        // Act
        var result = await _removeSut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("COMPANY_USER_EXCEPTION_NOT_FOUND");
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
}
