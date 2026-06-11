using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Invitation;

/// <summary>
/// Tests unitarios para ValidateInvitationQueryHandler.
/// AC2 de HU-9772: GET /invitations/{token}/validate → expirado → 400 INVITATION_EXPIRED.
/// </summary>
public class ValidateInvitationQueryHandlerTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

    private readonly ValidateInvitationQueryHandler _sut;

    public ValidateInvitationQueryHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new ValidateInvitationQueryHandler(_invitationRepo, _roleRepo, _clock);
    }

    // ─── AC2: Token válido ────────────────────────────────────────────────────

    [Fact]
    public async Task AC2_ValidateToken_Valido_RetornaEmailYTenant()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var invitation = BuildInvitation(tenant, "invitado@acme.com", FixedNow.AddHours(72));
        var rawToken = "validtoken123";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        invitation.TokenHash = hash;

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(invitation);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role>());

        // Act
        var result = await _sut.HandleAsync(new ValidateInvitationQuery(rawToken));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("invitado@acme.com");
        result.Value.TenantName.Should().Be("Acme Corp");
        result.Value.TenantId.Should().Be(tenant.Id);
    }

    // ─── AC2: Token expirado → INVITATION_EXPIRED ────────────────────────────

    [Fact]
    public async Task AC2_ValidateToken_Expirado_RetornaInvitationExpired()
    {
        // Arrange: invitación expirada hace 1 hora
        var tenant = BuildTenant("t", "T");
        var invitation = BuildInvitation(tenant, "x@x.com", FixedNow.AddHours(-1));
        var rawToken = "expiredtoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        invitation.TokenHash = hash;

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _sut.HandleAsync(new ValidateInvitationQuery(rawToken));

        // Assert: AC2 — token expirado → INVITATION_EXPIRED
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVITATION_EXPIRED");
    }

    [Fact]
    public async Task AC2_ValidateToken_NoExiste_RetornaInvitationNotFound()
    {
        // Arrange
        _invitationRepo.FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((global::Flit.Infrastructure.Persistence.Entities.Identity.Invitation?)null);

        // Act
        var result = await _sut.HandleAsync(new ValidateInvitationQuery("noexiste"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVITATION_NOT_FOUND");
    }

    [Fact]
    public async Task AC2_ValidateToken_YaUsado_RetornaInvitationAlreadyUsed()
    {
        // Arrange
        var tenant = BuildTenant("t", "T");
        var invitation = BuildInvitation(tenant, "x@x.com", FixedNow.AddHours(24));
        invitation.Status = "accepted";
        var rawToken = "usedtoken";
        var hash = Flit.Modules.Identity.Application.TokenHelper.Hash(rawToken);
        invitation.TokenHash = hash;

        _invitationRepo.FindByTokenHashAsync(hash, Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _sut.HandleAsync(new ValidateInvitationQuery(rawToken));

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
        Tenant tenant, string email, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant.Id,
        Tenant = tenant,
        Email = email,
        TokenHash = string.Empty,
        RolesJson = "[]",
        Status = "pending",
        ExpiresAt = expiresAt,
        CreatedAt = FixedNow
    };
}
