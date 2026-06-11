using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rbac;

/// <summary>
/// Tests unitarios para CreateRoleCommandHandler.
/// AC1 HU-9770: POST /api/v1/roles crea rol en identity.roles.
/// </summary>
public class CreateRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository _permRepo = Substitute.For<IPermissionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly CreateRoleCommandHandler _sut;

    public CreateRoleCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateRoleCommandHandler(_roleRepo, _permRepo, _clock);
    }

    // ─── AC1: creación exitosa ────────────────────────────────────────────────

    [Fact]
    public async Task AC1_CrearRol_SlugNuevo_RetornaRolCreado()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var perm = BuildPermission(permId, "users.read");

        _roleRepo.SlugExistsAsync("admin", tenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _permRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([perm]);

        var command = new CreateRoleCommand(tenantId, userId, "admin", "Administrador", null, [permId]);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("admin");
        result.Value.Name.Should().Be("Administrador");
        result.Value.IsSystem.Should().BeFalse("roles creados por API nunca son de sistema");
        result.Value.Permissions.Should().Contain("users.read");
    }

    [Fact]
    public async Task AC1_CrearRol_SlugNuevo_PersisteFila()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _roleRepo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _permRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = new CreateRoleCommand(tenantId, Guid.NewGuid(), "viewer", "Visor", null, []);

        // Act
        await _sut.HandleAsync(command);

        // Assert: debe persistir en repositorio
        await _roleRepo.Received(1).CreateAsync(
            Arg.Is<Role>(r => r.Slug == "viewer" && r.TenantId == tenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CrearRol_TenantIdEsCorrectoEnRol()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _roleRepo.SlugExistsAsync(Arg.Any<string>(), tenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _permRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = new CreateRoleCommand(tenantId, Guid.NewGuid(), "op", "Operador", null, []);

        // Act
        await _sut.HandleAsync(command);

        // Assert: el rol creado lleva el tenant_id del JWT
        await _roleRepo.Received(1).CreateAsync(
            Arg.Is<Role>(r => r.TenantId == tenantId),
            Arg.Any<CancellationToken>());
    }

    // ─── Slug duplicado ───────────────────────────────────────────────────────

    [Fact]
    public async Task AC1_CrearRol_SlugDuplicado_RetornaError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _roleRepo.SlugExistsAsync("admin", tenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateRoleCommand(tenantId, Guid.NewGuid(), "admin", "Admin", null, []);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ROLE_SLUG_ALREADY_EXISTS");
    }

    [Fact]
    public async Task AC1_CrearRol_SlugDuplicado_NoPersiste()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _roleRepo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.HandleAsync(new CreateRoleCommand(tenantId, Guid.NewGuid(), "dup", "Dup", null, []));

        // Assert
        await _roleRepo.DidNotReceive().CreateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Permission BuildPermission(Guid id, string slug) => new()
    {
        Id = id,
        Slug = slug,
        Module = slug.Split('.')[0],
        Action = slug.Split('.')[1],
        RolePermissions = []
    };
}
