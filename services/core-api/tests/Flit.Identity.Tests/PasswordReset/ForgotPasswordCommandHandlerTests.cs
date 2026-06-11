using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.PasswordReset;

/// <summary>
/// Tests unitarios para ForgotPasswordCommandHandler.
/// AC3 de HU-9772: POST /auth/forgot-password siempre 204 (no revelar existencia de email).
/// </summary>
public class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _resetTokenRepo = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new ForgotPasswordCommandHandler(_userRepo, _resetTokenRepo, _emailSender, _clock);
    }

    // ─── AC3: Usuario existe y activo ─────────────────────────────────────────

    [Fact]
    public async Task AC3_ForgotPassword_UsuarioActivo_CreaTokenYEnviaEmail()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var user = BuildUser(tenant, "admin@acme.com", "active");

        _userRepo.FindByEmailAndTenantSlugAsync("admin@acme.com", "acme", Arg.Any<CancellationToken>())
            .Returns(user);

        // Act — no debe lanzar excepción
        await _sut.HandleAsync(new ForgotPasswordCommand("admin@acme.com", "acme"));

        // Assert
        await _resetTokenRepo.Received(1).CreateAsync(
            Arg.Is<PasswordResetToken>(t =>
                t.UserId == user.Id &&
                !string.IsNullOrEmpty(t.TokenHash) &&
                t.UsedAt == null),
            Arg.Any<CancellationToken>());

        await _emailSender.Received(1).SendAsync(
            "admin@acme.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ForgotPassword_TokenExpiraEn24Horas()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com", "active");

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.HandleAsync(new ForgotPasswordCommand("u@t.com", "t"));

        // Assert: token debe expirar en ~24h
        await _resetTokenRepo.Received(1).CreateAsync(
            Arg.Is<PasswordResetToken>(t =>
                t.ExpiresAt >= FixedNow.AddHours(23) &&
                t.ExpiresAt <= FixedNow.AddHours(25)),
            Arg.Any<CancellationToken>());
    }

    // ─── AC3: Email no existe → silencio (204 igualmente) ────────────────────

    [Fact]
    public async Task AC3_ForgotPassword_EmailNoExiste_NoEnviaEmailNiCrearToken()
    {
        // Arrange
        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act — no debe lanzar
        await _sut.HandleAsync(new ForgotPasswordCommand("nobody@x.com", "tenant"));

        // Assert: no se debe revelar existencia
        await _resetTokenRepo.DidNotReceive().CreateAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ForgotPassword_UsuarioSuspendido_NoEnviaEmail()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var user = BuildUser(tenant, "u@t.com", "suspended");

        _userRepo.FindByEmailAndTenantSlugAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _sut.HandleAsync(new ForgotPasswordCommand("u@t.com", "t"));

        // Assert
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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

    private static User BuildUser(Tenant tenant, string email, string status) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        FullName = "Test User",
        PasswordHash = "hash",
        Status = status,
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid()
    };
}
