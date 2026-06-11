using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rbac;

/// <summary>
/// Tests unitarios para AssignUserRolesCommandHandler.
/// AC1 HU-9770: PATCH /users/{userId}/roles reemplaza roles y revoca sesiones activas.
/// AC1 HU-9771: is_revoked=true, JTI en blacklist → siguiente request retorna 403.
/// AC2 HU-9771: notificación SignalR SessionRevoked {reason: roles_changed} al usuario afectado.
/// </summary>
public class AssignUserRolesCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly ISessionBlacklist _blacklist = Substitute.For<ISessionBlacklist>();
    private readonly ISessionNotifier _notifier = Substitute.For<ISessionNotifier>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly AssignUserRolesCommandHandler _sut;

    public AssignUserRolesCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new AssignUserRolesCommandHandler(
            _userRepo, _roleRepo, _sessionRepo, _blacklist, _notifier, _clock);
    }

    // ─── AC1: asignación exitosa ──────────────────────────────────────────────

    [Fact]
    public async Task AC1_AsignarRoles_UsuarioExiste_RetornaRolesActualizados()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = BuildUser(tenantId);
        var role = BuildRole(tenantId, "admin", "Administrador");

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([role]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Session>());

        var command = new AssignUserRolesCommand(user.Id, tenantId, adminId, [role.Id]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Roles.Should().HaveCount(1);
        result.Value.Roles[0].Slug.Should().Be("admin");
    }

    [Fact]
    public async Task AC1_AsignarRoles_RevocaSesionesActivas()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);
        var session1 = BuildSession(user.Id, tenantId, "jti-001");
        var session2 = BuildSession(user.Id, tenantId, "jti-002");

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([session1, session2]);

        var command = new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []);

        // Act
        await _sut.HandleAsync(command);

        // Assert: ambas sesiones deben revocarse
        await _sessionRepo.Received(1).RevokeAsync(
            Arg.Is<Session>(s => s.Jti == "jti-001"),
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _sessionRepo.Received(1).RevokeAsync(
            Arg.Is<Session>(s => s.Jti == "jti-002"),
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_AsignarRoles_AgregaJtiABlacklist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);
        var session = BuildSession(user.Id, tenantId, "jti-revoked");

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([session]);

        var command = new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []);

        // Act
        await _sut.HandleAsync(command);

        // Assert: JTI debe ingresar en la blacklist inmediatamente
        await _blacklist.Received(1).RevokeAsync(
            "jti-revoked",
            session.ExpiresAt,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_AsignarRoles_SinSesionesActivas_NoRevocaNada()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _sut.HandleAsync(new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []));

        // Assert: nada que revocar
        await _sessionRepo.DidNotReceive().RevokeAsync(
            Arg.Any<Session>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _blacklist.DidNotReceive().RevokeAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // ─── AC2 HU-9771: notificación SignalR ───────────────────────────────────

    [Fact]
    public async Task AC2_AsignarRoles_ConSesionesActivas_NotificaSessionRevocada()
    {
        // Arrange: usuario con una sesión activa
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);
        var session = BuildSession(user.Id, tenantId, "jti-active");

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([session]);

        var command = new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []);

        // Act
        await _sut.HandleAsync(command);

        // Assert: notificador debe recibir el userId y reason "roles_changed"
        await _notifier.Received(1).NotifySessionRevokedAsync(
            user.Id,
            "roles_changed",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_AsignarRoles_SinSesionesActivas_NoNotifica()
    {
        // Arrange: usuario sin sesiones activas
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _sut.HandleAsync(new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []));

        // Assert: sin sesiones activas no hay nada que notificar
        await _notifier.DidNotReceive().NotifySessionRevokedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_AsignarRoles_MultiplesSessiones_NotificaUnaVez()
    {
        // Arrange: dos sesiones activas → una sola notificación SignalR al usuario
        var tenantId = Guid.NewGuid();
        var user = BuildUser(tenantId);
        var s1 = BuildSession(user.Id, tenantId, "jti-a");
        var s2 = BuildSession(user.Id, tenantId, "jti-b");

        _userRepo.FindByIdWithRolesAsync(user.Id, tenantId, Arg.Any<CancellationToken>())
            .Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), tenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionRepo.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([s1, s2]);

        // Act
        await _sut.HandleAsync(new AssignUserRolesCommand(user.Id, tenantId, Guid.NewGuid(), []));

        // Assert: aunque hay 2 sesiones, el evento al usuario es único
        await _notifier.Received(1).NotifySessionRevokedAsync(
            user.Id, "roles_changed", Arg.Any<CancellationToken>());
    }

    // ─── Usuario no encontrado ────────────────────────────────────────────────

    [Fact]
    public async Task AC1_AsignarRoles_UsuarioNoExiste_RetornaUserNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _userRepo.FindByIdWithRolesAsync(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new AssignUserRolesCommand(userId, tenantId, Guid.NewGuid(), []);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task AC1_AsignarRoles_UsuarioNoExiste_NoNotifica()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _userRepo.FindByIdWithRolesAsync(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        await _sut.HandleAsync(new AssignUserRolesCommand(userId, tenantId, Guid.NewGuid(), []));

        // Assert: no debe notificar si el usuario no existe
        await _notifier.DidNotReceive().NotifySessionRevokedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static User BuildUser(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Email = "test@tenant.com",
        FullName = "Test User",
        PasswordHash = "hash",
        Status = "active",
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        UserRoles = [],
        Sessions = []
    };

    private static Role BuildRole(Guid tenantId, string slug, string name) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Slug = slug,
        Name = name,
        IsSystem = false,
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        RolePermissions = [],
        UserRoles = []
    };

    private static Session BuildSession(Guid userId, Guid tenantId, string jti) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = tenantId,
        Jti = jti,
        ExpiresAt = FixedNow.AddMinutes(10),
        IsRevoked = false,
        CreatedAt = FixedNow
    };
}
