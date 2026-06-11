using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rbac;

/// <summary>
/// Tests unitarios para ListRolesQueryHandler.
/// AC2 HU-9770: GET /roles devuelve solo roles del tenant del JWT (aislamiento multi-tenant).
/// </summary>
public class ListRolesQueryHandlerTests
{
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ListRolesQueryHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    public ListRolesQueryHandlerTests()
    {
        _sut = new ListRolesQueryHandler(_roleRepo);
    }

    // ─── AC2: aislamiento por tenant ─────────────────────────────────────────

    [Fact]
    public async Task AC2_ListarRoles_SoloDevuelveRolesDelTenant()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var rolesA = new List<Role>
        {
            BuildRole(tenantA, "admin", "Administrador"),
            BuildRole(tenantA, "viewer", "Visor")
        };

        // El repositorio ya filtra por tenantId — solo devuelve roles del tenant A
        _roleRepo.ListByTenantAsync(tenantA, Arg.Any<CancellationToken>())
            .Returns(rolesA);

        // Act
        var result = await _sut.HandleAsync(new ListRolesQuery(tenantA));

        // Assert
        result.Should().HaveCount(2, "solo se deben devolver los roles del tenant A");
        result.Should().NotContain(r => r.Id == Guid.Empty, "todos los roles deben tener id válido");
        result.Select(r => r.Slug).Should().BeEquivalentTo(["admin", "viewer"]);
    }

    [Fact]
    public async Task AC2_ListarRoles_TenantDiferente_NoCruzaDatos()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        _roleRepo.ListByTenantAsync(tenantA, Arg.Any<CancellationToken>())
            .Returns([BuildRole(tenantA, "admin", "Admin")]);
        _roleRepo.ListByTenantAsync(tenantB, Arg.Any<CancellationToken>())
            .Returns([BuildRole(tenantB, "superuser", "Super")]);

        // Act
        var resultA = await _sut.HandleAsync(new ListRolesQuery(tenantA));
        var resultB = await _sut.HandleAsync(new ListRolesQuery(tenantB));

        // Assert: ningún rol del tenant B aparece en la respuesta del tenant A
        var idsA = resultA.Select(r => r.Id).ToHashSet();
        var idsB = resultB.Select(r => r.Id).ToHashSet();
        idsA.Intersect(idsB).Should().BeEmpty("no debe haber cruce de roles entre tenants");
    }

    [Fact]
    public async Task AC2_ListarRoles_TenantSinRoles_RetornaListaVacia()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _roleRepo.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.HandleAsync(new ListRolesQuery(tenantId));

        // Assert
        result.Should().BeEmpty("tenant sin roles debe retornar array vacío");
    }

    [Fact]
    public async Task AC2_ListarRoles_PermisosDelRolSeIncluyen()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var perm = new Permission { Id = Guid.NewGuid(), Slug = "users.read", Module = "users", Action = "read", RolePermissions = [] };
        var role = BuildRole(tenantId, "viewer", "Visor");
        role.RolePermissions = [new RolePermission { RoleId = role.Id, PermissionId = perm.Id, Role = role, Permission = perm }];

        _roleRepo.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns([role]);

        // Act
        var result = await _sut.HandleAsync(new ListRolesQuery(tenantId));

        // Assert
        result.Should().HaveCount(1);
        result[0].Permissions.Should().Contain("users.read");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

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
}
