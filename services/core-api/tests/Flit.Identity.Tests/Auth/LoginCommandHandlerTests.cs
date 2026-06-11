using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Auth;

/// <summary>
/// Tests unitarios para LoginCommandHandler.
/// AC1: login exitoso → JWT + sesión registrada.
/// AC2: password incorrecto → 401 INVALID_CREDENTIALS, sin sesión.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer _tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly LoginCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new LoginCommandHandler(
            _userRepo, _sessionRepo, _passwordHasher, _tokenIssuer, _clock);
    }

    // ─── AC1: Login exitoso ──────────────────────────────────────────────────

    [Fact]
    public async Task AC1_Login_CredencialesCorrectas_RetornaTokenYPerfil()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var role = BuildRole(tenant.Id, "admin", "Administrador");
        var perm = BuildPermission("users.read");
        role.RolePermissions = [new RolePermission { Role = role, Permission = perm }];

        var user = BuildUser(tenant, "admin@acme.com", "hash123", "active");
        user.UserRoles = [new UserRole { UserId = user.Id, RoleId = role.Id, Role = role, TenantId = tenant.Id }];

        var tokenResult = new TokenResult("token-jwt-rs256", "jti-abc", FixedNow.AddMinutes(15), 900);

        _userRepo.FindByEmailAndTenantSlugAsync("admin@acme.com", "acme", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("Flit2026@Dev!", "hash123").Returns(true);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(tokenResult);

        // Act
        var result = await _sut.HandleAsync(
            new LoginCommand("admin@acme.com", "Flit2026@Dev!", "acme"));

        // Assert
        result.IsSuccess.Should().BeTrue("login con credenciales correctas debe ser exitoso");
        result.Value.AccessToken.Should().Be("token-jwt-rs256");
        result.Value.ExpiresIn.Should().Be(900, "JWT debe tener vida de 15 min (900s)");
        result.Value.User.Email.Should().Be("admin@acme.com");
        result.Value.User.Roles.Should().Contain("admin");
        result.Value.User.Permissions.Should().Contain("users.read");
        result.Value.User.TenantId.Should().Be(tenant.Id);
        result.Value.User.TenantName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task AC1_Login_RegistraSesionEnBD()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var user = BuildUser(tenant, "admin@acme.com", "hash123", "active");
        var tokenResult = new TokenResult("token", "jti-xyz", FixedNow.AddMinutes(15), 900);

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(tokenResult);

        // Act
        await _sut.HandleAsync(new LoginCommand("admin@acme.com", "Flit2026@Dev!", "acme"));

        // Assert: sesión debe haberse registrado en BD
        await _sessionRepo.Received(1).CreateAsync(
            Arg.Is<Session>(s =>
                s.Jti == "jti-xyz" &&
                s.UserId == user.Id &&
                s.TenantId == tenant.Id &&
                s.IsRevoked == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_Login_TokenContieneJtiUnico()
    {
        // Arrange
        var tenant = BuildTenant("t1", "Tenant");
        var user = BuildUser(tenant, "u@t.com", "h", "active");
        var jti = Guid.NewGuid().ToString("N");
        var tokenResult = new TokenResult("tok", jti, FixedNow.AddMinutes(15), 900);

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenIssuer.Issue(Arg.Any<TokenRequest>()).Returns(tokenResult);

        // Act
        var result = await _sut.HandleAsync(new LoginCommand("u@t.com", "pwd", "t1"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokenIssuer.Received(1).Issue(Arg.Is<TokenRequest>(r =>
            r.UserId == user.Id &&
            r.TenantId == tenant.Id &&
            r.Email == user.Email));
    }

    // ─── AC2: Password incorrecto ────────────────────────────────────────────

    [Fact]
    public async Task AC2_Login_PasswordIncorrecto_RetornaInvalidCredentials()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var user = BuildUser(tenant, "admin@acme.com", "hash123", "active");

        _userRepo.FindByEmailAndTenantSlugAsync("admin@acme.com", "acme", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify("WrongPassword!", "hash123").Returns(false);

        // Act
        var result = await _sut.HandleAsync(
            new LoginCommand("admin@acme.com", "WrongPassword!", "acme"));

        // Assert
        result.IsSuccess.Should().BeFalse("password incorrecto debe fallar");
        result.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task AC2_Login_PasswordIncorrecto_NoRegistraSesion()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var user = BuildUser(tenant, "admin@acme.com", "hash123", "active");

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        await _sut.HandleAsync(new LoginCommand("admin@acme.com", "WrongPassword!", "acme"));

        // Assert: NO debe registrar sesión
        await _sessionRepo.DidNotReceive().CreateAsync(
            Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_Login_PasswordIncorrecto_NoEmiteToken()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var user = BuildUser(tenant, "admin@acme.com", "hash123", "active");

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        await _sut.HandleAsync(new LoginCommand("admin@acme.com", "Wrong!", "acme"));

        // Assert: NO debe emitir JWT
        _tokenIssuer.DidNotReceive().Issue(Arg.Any<TokenRequest>());
    }

    [Fact]
    public async Task AC2_Login_UsuarioNoExiste_RetornaInvalidCredentials()
    {
        // Arrange: usuario no encontrado
        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _sut.HandleAsync(
            new LoginCommand("nobody@x.com", "pass", "tenant"));

        // Assert: mismo error para no revelar existencia del usuario
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task AC2_Login_UsuarioSuspendido_RetornaUserNotActive()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com", "h", "suspended");

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.HandleAsync(new LoginCommand("u@t.com", "p", "t"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("USER_NOT_ACTIVE");
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

    private static User BuildUser(Tenant tenant, string email, string passwordHash, string status) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        FullName = "Test User",
        PasswordHash = passwordHash,
        Status = status,
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        UserRoles = []
    };

    private static Role BuildRole(Guid tenantId, string slug, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Slug = slug,
        Name = name,
        IsSystem = true,
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        RolePermissions = [],
        UserRoles = []
    };

    private static Permission BuildPermission(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Module = "users",
        Action = "read",
        RolePermissions = []
    };
}
