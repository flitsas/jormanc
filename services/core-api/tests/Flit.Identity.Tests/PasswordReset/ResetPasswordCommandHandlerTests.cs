using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.PasswordReset;

/// <summary>
/// Tests unitarios para ResetPasswordCommandHandler.
/// AC3 de HU-9772: actualiza hash + token.used_at; segundo uso → RESET_TOKEN_ALREADY_USED.
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    private readonly IPasswordResetTokenRepository _resetTokenRepo = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new ResetPasswordCommandHandler(_resetTokenRepo, _userRepo, _passwordHasher, _clock);
    }

    // ─── AC3: Reset exitoso ───────────────────────────────────────────────────

    [Fact]
    public async Task AC3_ResetPassword_TokenValido_ActualizaHashYMarcaUsedAt()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com");
        var rawToken = "resettoken123";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var resetToken = BuildResetToken(user, hash, FixedNow.AddHours(24), usedAt: null);

        _resetTokenRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _passwordHasher.Hash("NewPwd456@").Returns("newhash");

        // Act
        var result = await _sut.HandleAsync(new ResetPasswordCommand(rawToken, "NewPwd456@"));

        // Assert
        result.IsSuccess.Should().BeTrue("token válido debe permitir el reset");
        user.PasswordHash.Should().Be("newhash", "el hash del password debe actualizarse");
        resetToken.UsedAt.Should().NotBeNull("used_at debe quedar registrado");
    }

    [Fact]
    public async Task AC3_ResetPassword_TokenValido_PersisteCambios()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com");
        var rawToken = "token-persist";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var resetToken = BuildResetToken(user, hash, FixedNow.AddHours(24), usedAt: null);

        _resetTokenRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("h");

        // Act
        await _sut.HandleAsync(new ResetPasswordCommand(rawToken, "P@ssw0rd!"));

        // Assert
        await _resetTokenRepo.Received(1).UpdateAsync(resetToken, Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(Arg.Any<CancellationToken>());
    }

    // ─── AC3: Segundo uso → RESET_TOKEN_ALREADY_USED ─────────────────────────

    [Fact]
    public async Task AC3_ResetPassword_SegundoUso_RetornaTokenAlreadyUsed()
    {
        // Arrange: token con used_at ya establecido (ya fue usado antes)
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com");
        var rawToken = "usedresettoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var resetToken = BuildResetToken(user, hash, FixedNow.AddHours(24), usedAt: FixedNow.AddHours(-1));

        _resetTokenRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(resetToken);

        // Act
        var result = await _sut.HandleAsync(new ResetPasswordCommand(rawToken, "Pwd123!"));

        // Assert
        result.IsSuccess.Should().BeFalse("token ya usado debe fallar");
        result.Error.Code.Should().Be("RESET_TOKEN_ALREADY_USED");
    }

    [Fact]
    public async Task AC3_ResetPassword_SegundoUso_NoActualizaPasswordHash()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com");
        var rawToken = "usedtoken2";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var resetToken = BuildResetToken(user, hash, FixedNow.AddHours(24), usedAt: FixedNow.AddMinutes(-30));

        _resetTokenRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(resetToken);

        // Act
        await _sut.HandleAsync(new ResetPasswordCommand(rawToken, "NewPwd!"));

        // Assert: no debe actualizar
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    // ─── AC3: Token expirado ──────────────────────────────────────────────────

    [Fact]
    public async Task AC3_ResetPassword_TokenExpirado_RetornaResetTokenExpired()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com");
        var rawToken = "expiredresettoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        var resetToken = BuildResetToken(user, hash, FixedNow.AddHours(-2), usedAt: null);

        _resetTokenRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>()).Returns(resetToken);

        // Act
        var result = await _sut.HandleAsync(new ResetPasswordCommand(rawToken, "Pwd!"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESET_TOKEN_EXPIRED");
    }

    [Fact]
    public async Task AC3_ResetPassword_TokenNoExiste_RetornaResetTokenNotFound()
    {
        // Arrange
        _resetTokenRepo.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        // Act
        var result = await _sut.HandleAsync(new ResetPasswordCommand("notoken", "Pwd!"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESET_TOKEN_NOT_FOUND");
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

    private static User BuildUser(Tenant tenant, string email) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        FullName = "Test User",
        PasswordHash = "oldhash",
        Status = "active",
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid()
    };

    private static PasswordResetToken BuildResetToken(
        User user, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset? usedAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        User = user,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt,
        UsedAt = usedAt,
        CreatedAt = FixedNow
    };
}
