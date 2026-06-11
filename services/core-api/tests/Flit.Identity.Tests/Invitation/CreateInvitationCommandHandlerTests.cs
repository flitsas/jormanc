using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Invitation;

/// <summary>
/// Tests unitarios para CreateInvitationCommandHandler.
/// AC1 de HU-9772: POST /invitations crea invitación y envía email.
/// </summary>
public class CreateInvitationCommandHandlerTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly CreateInvitationCommandHandler _sut;

    public CreateInvitationCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateInvitationCommandHandler(
            _invitationRepo, _userRepo, _emailSender, _clock);
    }

    // ─── AC1: Crear invitación exitosa ────────────────────────────────────────

    [Fact]
    public async Task AC1_CreateInvitation_EmailNuevo_RetornaInvitacionPendiente()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var invitedBy = Guid.NewGuid();
        var command = new CreateInvitationCommand("nuevo@empresa.com", [], tenantId, invitedBy);

        _userRepo.EmailExistsAsync("nuevo@empresa.com", tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue("email nuevo debe generar invitación");
        result.Value.Email.Should().Be("nuevo@empresa.com");
        result.Value.Status.Should().Be("pending");
        result.Value.ExpiresAt.Should().BeAfter(FixedNow);
    }

    [Fact]
    public async Task AC1_CreateInvitation_PersisteLaInvitacionEnBD()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateInvitationCommand("x@x.com", [], tenantId, Guid.NewGuid());
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _invitationRepo.Received(1).CreateAsync(
            Arg.Is<global::Flit.Infrastructure.Persistence.Entities.Identity.Invitation>(i =>
                i.Email == "x@x.com" &&
                i.TenantId == tenantId &&
                i.Status == "pending" &&
                !string.IsNullOrEmpty(i.TokenHash)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CreateInvitation_EnviaEmail()
    {
        // Arrange
        var command = new CreateInvitationCommand("test@t.com", [], Guid.NewGuid(), Guid.NewGuid());
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _emailSender.Received(1).SendAsync(
            "test@t.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CreateInvitation_EmailExpiresEn72Horas()
    {
        // Arrange
        var command = new CreateInvitationCommand("x@x.com", [], Guid.NewGuid(), Guid.NewGuid());
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.Value.ExpiresAt.Should().BeCloseTo(FixedNow.AddHours(72), TimeSpan.FromSeconds(5));
    }

    // ─── AC1: Email ya registrado ────────────────────────────────────────────

    [Fact]
    public async Task AC1_CreateInvitation_EmailExistente_RetornaEmailAlreadyRegistered()
    {
        // Arrange
        var command = new CreateInvitationCommand("existe@empresa.com", [], Guid.NewGuid(), Guid.NewGuid());
        _userRepo.EmailExistsAsync("existe@empresa.com", Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EMAIL_ALREADY_REGISTERED");
    }

    [Fact]
    public async Task AC1_CreateInvitation_EmailExistente_NoEnviaEmail()
    {
        // Arrange
        var command = new CreateInvitationCommand("existe@empresa.com", [], Guid.NewGuid(), Guid.NewGuid());
        _userRepo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
