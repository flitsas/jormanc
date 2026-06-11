using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Invitation;

/// <summary>
/// Tests unitarios para AcceptInvitationCommandHandler.
/// AC1 de HU-9772: POST /invitations/{token}/accept → usuario active + access_token.
/// </summary>
public class AcceptInvitationCommandHandlerTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer _tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly AcceptInvitationCommandHandler _sut;

    public AcceptInvitationCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new AcceptInvitationCommandHandler(
            _invitationRepo, _userRepo, _roleRepo, _sessionRepo,
            _passwordHasher, _tokenIssuer, _clock);
    }

    // ─── AC1: Aceptar invitación válida ───────────────────────────────────────

    [Fact]
    public async Task AC1_AcceptInvitation_TokenValido_CreaUsuarioYRetornaToken()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var rawToken = "validtoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var invitation = BuildInvitation(tenant, "nuevo@acme.com", FixedNow.AddHours(72), hash);

        var createdUser = BuildUser(tenant, "nuevo@acme.com");
        var tokenResult = new TokenResult("jwt-token", "jti-123", FixedNow.AddMinutes(15), 900);

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(invitation);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role>());
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashedpwd");
        _userRepo.FindByIdWithRolesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(tokenResult);

        // Act
        var result = await _sut.HandleAsync(
            new AcceptInvitationCommand(rawToken, "Nuevo Usuario", "Flit2026@Dev!"));

        // Assert
        result.IsSuccess.Should().BeTrue("invitación válida debe crear usuario y retornar JWT");
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.ExpiresIn.Should().Be(900);
        result.Value.User.Email.Should().Be("nuevo@acme.com");
    }

    [Fact]
    public async Task AC1_AcceptInvitation_MarcaInvitacionComoAceptada()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var rawToken = "token1";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var invitation = BuildInvitation(tenant, "u@t.com", FixedNow.AddHours(24), hash);

        var createdUser = BuildUser(tenant, "u@t.com");
        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invitation);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role>());
        _passwordHasher.Hash(Arg.Any<string>()).Returns("h");
        _userRepo.FindByIdWithRolesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(
            new TokenResult("tok", "jti", FixedNow.AddMinutes(15), 900));

        // Act
        await _sut.HandleAsync(new AcceptInvitationCommand(rawToken, "Juan", "Pwd123!"));

        // Assert: invitación debe quedar como accepted
        await _invitationRepo.Received(1).UpdateAsync(
            Arg.Is<global::Flit.Infrastructure.Persistence.Entities.Identity.Invitation>(i =>
                i.Status == "accepted" && i.AcceptedAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_AcceptInvitation_CreaUserYRegistraSesion()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var rawToken = "token2";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var invitation = BuildInvitation(tenant, "u@t.com", FixedNow.AddHours(24), hash);

        var createdUser = BuildUser(tenant, "u@t.com");
        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invitation);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role>());
        _passwordHasher.Hash(Arg.Any<string>()).Returns("h");
        _userRepo.FindByIdWithRolesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(
            new TokenResult("tok", "jti-456", FixedNow.AddMinutes(15), 900));

        // Act
        await _sut.HandleAsync(new AcceptInvitationCommand(rawToken, "Ana", "Pwd123!"));

        // Assert
        await _userRepo.Received(1).CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).CreateAsync(
            Arg.Is<Session>(s => s.Jti == "jti-456" && !s.IsRevoked),
            Arg.Any<CancellationToken>());
    }

    // ─── AC1: Token expirado ──────────────────────────────────────────────────

    [Fact]
    public async Task AC1_AcceptInvitation_TokenExpirado_RetornaInvitationExpired()
    {
        // Arrange: expirado hace 1 hora
        var tenant = BuildTenant("t", "T");
        var rawToken = "exptoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var invitation = BuildInvitation(tenant, "x@x.com", FixedNow.AddHours(-1), hash);

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invitation);

        // Act
        var result = await _sut.HandleAsync(new AcceptInvitationCommand(rawToken, "X", "P"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVITATION_EXPIRED");
    }

    [Fact]
    public async Task AC1_AcceptInvitation_InvitacionYaAceptada_RetornaAlreadyUsed()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var rawToken = "usedtoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var invitation = BuildInvitation(tenant, "x@x.com", FixedNow.AddHours(24), hash);
        invitation.Status = "accepted";

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invitation);

        // Act
        var result = await _sut.HandleAsync(new AcceptInvitationCommand(rawToken, "X", "P"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVITATION_ALREADY_USED");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Tenant BuildTenant(string slug, string name) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = name,
        IsActive = true,
        CreatedAt = FixedNow,
        UpdatedAt = FixedNow
    };

    private static global::Flit.Infrastructure.Persistence.Entities.Identity.Invitation BuildInvitation(
        Tenant tenant, string email, DateTimeOffset expiresAt, string tokenHash) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        TokenHash = tokenHash,
        RolesJson = "[]",
        Status = "pending",
        ExpiresAt = expiresAt,
        CreatedAt = FixedNow
    };

    private static User BuildUser(Tenant tenant, string email) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        FullName = "Test User",
        PasswordHash = "hashedpwd",
        Status = "active",
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        UserRoles = []
    };
}
