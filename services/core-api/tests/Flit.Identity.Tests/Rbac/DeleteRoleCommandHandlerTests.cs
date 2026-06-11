using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rbac;

/// <summary>
/// Tests unitarios para DeleteRoleCommandHandler.
/// AC3 HU-9770: DELETE /roles/{id} con is_system=true → HTTP 409 SYSTEM_ROLE_CANNOT_BE_DELETED.
/// </summary>
public class DeleteRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly DeleteRoleCommandHandler _sut;

    public DeleteRoleCommandHandlerTests()
    {
        _sut = new DeleteRoleCommandHandler(_roleRepo);
    }

    // ─── AC3: rol de sistema no se puede eliminar ─────────────────────────────

    [Fact]
    public async Task AC3_EliminarRol_IsSystemTrue_RetornaSystemRoleCannotBeDeleted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = BuildRole(roleId, tenantId, "superadmin", isSystem: true);

        _roleRepo.FindByIdAsync(roleId, tenantId, Arg.Any<CancellationToken>())
            .Returns(role);

        var command = new DeleteRoleCommand(roleId, tenantId, Guid.NewGuid());

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse("rol de sistema no puede eliminarse");
        result.Error.Code.Should().Be("SYSTEM_ROLE_CANNOT_BE_DELETED");
    }

    [Fact]
    public async Task AC3_EliminarRol_IsSystemTrue_NoEliminaDelRepositorio()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = BuildRole(roleId, tenantId, "superadmin", isSystem: true);

        _roleRepo.FindByIdAsync(roleId, tenantId, Arg.Any<CancellationToken>())
            .Returns(role);

        // Act
        await _sut.HandleAsync(new DeleteRoleCommand(roleId, tenantId, Guid.NewGuid()));

        // Assert: no debe llamar a DeleteAsync
        await _roleRepo.DidNotReceive().DeleteAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    // ─── Eliminación exitosa ─────────────────────────────────────────────────

    [Fact]
    public async Task AC3_EliminarRol_IsSystemFalse_EliminacionExitosa()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = BuildRole(roleId, tenantId, "viewer", isSystem: false);

        _roleRepo.FindByIdAsync(roleId, tenantId, Arg.Any<CancellationToken>())
            .Returns(role);

        var command = new DeleteRoleCommand(roleId, tenantId, Guid.NewGuid());

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue("rol no-sistema puede eliminarse");
        await _roleRepo.Received(1).DeleteAsync(
            Arg.Is<Role>(r => r.Id == roleId),
            Arg.Any<CancellationToken>());
    }

    // ─── Rol no encontrado ────────────────────────────────────────────────────

    [Fact]
    public async Task AC3_EliminarRol_NoExiste_RetornaRoleNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _roleRepo.FindByIdAsync(roleId, tenantId, Arg.Any<CancellationToken>())
            .Returns((Role?)null);

        var command = new DeleteRoleCommand(roleId, tenantId, Guid.NewGuid());

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ROLE_NOT_FOUND");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Role BuildRole(Guid id, Guid tenantId, string slug, bool isSystem) => new()
    {
        Id = id,
        TenantId = tenantId,
        Slug = slug,
        Name = slug,
        IsSystem = isSystem,
        CreatedAt = FixedNow,
        CreatedBy = Guid.NewGuid(),
        UpdatedAt = FixedNow,
        UpdatedBy = Guid.NewGuid(),
        RolePermissions = [],
        UserRoles = []
    };
}
