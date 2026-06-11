using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.Modules.Identity.Infrastructure;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rehydration;

/// <summary>
/// Tests unitarios para BlacklistRehydrator.
/// AC3 HU-9771: JTIs revocados vigentes se cargan en ISessionBlacklist al arrancar.
/// </summary>
public class BlacklistRehydrationTests
{
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly ISessionBlacklist _blacklist = Substitute.For<ISessionBlacklist>();

    private static readonly DateTimeOffset Now = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private BlacklistRehydrator BuildSut() => new(_sessionRepo, _blacklist);

    // ─── AC3: rehydratación de JTIs al arrancar ───────────────────────────────

    [Fact]
    public async Task AC3_Rehydrate_SinSesionesRevocadas_NoLlamaRevokeAsync()
    {
        // Arrange: ninguna sesión revocada vigente en BD
        _sessionRepo.GetActiveRevokedForBlacklistAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = BuildSut();

        // Act
        var count = await sut.RehydrateAsync();

        // Assert
        count.Should().Be(0);
        await _blacklist.DidNotReceive().RevokeAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Rehydrate_UnaSesionRevocada_LlamaRevokeAsyncConJtiYExpiry()
    {
        // Arrange: una sesión revocada con expiración futura
        var expiresAt = Now.AddMinutes(10);
        var session = BuildRevokedSession("jti-revoked-001", expiresAt);

        _sessionRepo.GetActiveRevokedForBlacklistAsync(Arg.Any<CancellationToken>())
            .Returns([session]);

        var sut = BuildSut();

        // Act
        var count = await sut.RehydrateAsync();

        // Assert: el JTI debe agregarse a la blacklist con el TTL correcto
        count.Should().Be(1);
        await _blacklist.Received(1).RevokeAsync(
            "jti-revoked-001",
            expiresAt,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Rehydrate_MultiplesSesionesRevocadas_LlamaRevokeAsyncPorCadaJti()
    {
        // Arrange: tres sesiones revocadas vigentes
        var session1 = BuildRevokedSession("jti-001", Now.AddMinutes(5));
        var session2 = BuildRevokedSession("jti-002", Now.AddMinutes(10));
        var session3 = BuildRevokedSession("jti-003", Now.AddMinutes(14));

        _sessionRepo.GetActiveRevokedForBlacklistAsync(Arg.Any<CancellationToken>())
            .Returns([session1, session2, session3]);

        var sut = BuildSut();

        // Act
        var count = await sut.RehydrateAsync();

        // Assert: cada JTI debe registrarse independientemente en la blacklist
        count.Should().Be(3);
        await _blacklist.Received(1).RevokeAsync("jti-001", session1.ExpiresAt, Arg.Any<CancellationToken>());
        await _blacklist.Received(1).RevokeAsync("jti-002", session2.ExpiresAt, Arg.Any<CancellationToken>());
        await _blacklist.Received(1).RevokeAsync("jti-003", session3.ExpiresAt, Arg.Any<CancellationToken>());
        await _blacklist.Received(3).RevokeAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Rehydrate_RetornaConteoCorrectoDeSessionesCargadas()
    {
        // Arrange
        var sessions = Enumerable.Range(1, 5)
            .Select(i => BuildRevokedSession($"jti-{i:000}", Now.AddMinutes(i)))
            .ToArray();

        _sessionRepo.GetActiveRevokedForBlacklistAsync(Arg.Any<CancellationToken>())
            .Returns(sessions);

        var sut = BuildSut();

        // Act
        var count = await sut.RehydrateAsync();

        // Assert
        count.Should().Be(5, "deben cargarse exactamente 5 JTIs en la blacklist");
        await _blacklist.Received(5).RevokeAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Rehydrate_PropagaElJtiExactoQueVieneDeBD()
    {
        // Arrange: verifica que el JTI se pasa sin transformación a la blacklist
        var jti = Guid.NewGuid().ToString("N");
        var expiresAt = Now.AddMinutes(12);
        var session = BuildRevokedSession(jti, expiresAt);

        _sessionRepo.GetActiveRevokedForBlacklistAsync(Arg.Any<CancellationToken>())
            .Returns([session]);

        var sut = BuildSut();

        // Act
        await sut.RehydrateAsync();

        // Assert
        await _blacklist.Received(1).RevokeAsync(
            jti,
            expiresAt,
            Arg.Any<CancellationToken>());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Session BuildRevokedSession(string jti, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Jti = jti,
        ExpiresAt = expiresAt,
        IsRevoked = true,
        RevokedAt = Now.AddMinutes(-1),
        RevokedBy = Guid.NewGuid(),
        CreatedAt = Now.AddMinutes(-15)
    };
}
