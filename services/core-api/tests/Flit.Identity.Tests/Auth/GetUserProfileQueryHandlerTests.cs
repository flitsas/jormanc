using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Auth;

/// <summary>
/// Tests unitarios para GetUserProfileQueryHandler.
/// AC3: GET /auth/me con Bearer válido → perfil completo del usuario.
/// </summary>
public class GetUserProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly GetUserProfileQueryHandler _sut;

    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

    public GetUserProfileQueryHandlerTests()
    {
        _sut = new GetUserProfileQueryHandler(_userRepo);
    }

    // ─── AC3: GET /auth/me ───────────────────────────────────────────────────

    [Fact]
    public async Task AC3_GetMe_TokenValido_RetornaPerfilCompleto()
    {
        // Arrange
        var tenant = BuildTenant("acme", "Acme Corp");
        var role = BuildRole(tenant.Id, "admin", "Administrador");
        var perm = BuildPermission("tramites.create");
        role.RolePermissions = [new RolePermission { Role = role, Permission = perm }];

        var user = BuildUser(tenant, "admin@acme.com", "active");
        user.UserRoles = [new UserRole { UserId = user.Id, RoleId = role.Id, Role = role, TenantId = tenant.Id }];

        _userRepo.FindByIdWithRolesAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.HandleAsync(new GetUserProfileQuery(user.Id, tenant.Id));

        // Assert
        result.IsSuccess.Should().BeTrue("GET /auth/me con token válido debe retornar perfil");
        result.Value.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be(user.FullName);
        result.Value.Email.Should().Be("admin@acme.com");
        result.Value.Roles.Should().Contain("admin");
        result.Value.Permissions.Should().Contain("tramites.create");
        result.Value.TenantId.Should().Be(tenant.Id);
        result.Value.TenantName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task AC3_GetMe_UsuarioConMultiplesRoles_RetornaTodosLosPermisosUnicos()
    {
        // Arrange
        var tenant = BuildTenant("corp", "Corp SA");
        var roleAdmin = BuildRole(tenant.Id, "admin", "Admin");
        var roleOp = BuildRole(tenant.Id, "operator", "Operador");
        var permCreate = BuildPermission("tramites.create");
        var permRead = BuildPermission("tramites.read");
        roleAdmin.RolePermissions = [
            new RolePermission { Role = roleAdmin, Permission = permCreate },
            new RolePermission { Role = roleAdmin, Permission = permRead }
        ];
        roleOp.RolePermissions = [
            new RolePermission { Role = roleOp, Permission = permRead }  // duplicado intencional
        ];

        var user = BuildUser(tenant, "op@corp.com", "active");
        user.UserRoles = [
            new UserRole { UserId = user.Id, RoleId = roleAdmin.Id, Role = roleAdmin, TenantId = tenant.Id },
            new UserRole { UserId = user.Id, RoleId = roleOp.Id, Role = roleOp, TenantId = tenant.Id }
        ];

        _userRepo.FindByIdWithRolesAsync(user.Id, tenant.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.HandleAsync(new GetUserProfileQuery(user.Id, tenant.Id));

        // Assert: permisos deben ser únicos (no duplicados)
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEquivalentTo(["admin", "operator"]);
        result.Value.Permissions.Should().HaveCount(2,
            "tramites.read aparece en ambos roles pero debe deduplicarse");
        result.Value.Permissions.Should().BeEquivalentTo(["tramites.create", "tramites.read"]);
    }

    [Fact]
    public async Task AC3_GetMe_UsuarioNoExiste_RetornaError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _userRepo.FindByIdWithRolesAsync(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _sut.HandleAsync(new GetUserProfileQuery(userId, tenantId));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task AC3_GetMe_BuscaPorUserIdYTenantId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _userRepo.FindByIdWithRolesAsync(userId, tenantId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        await _sut.HandleAsync(new GetUserProfileQuery(userId, tenantId));

        // Assert: el repositorio fue llamado con los parámetros correctos (multi-tenant isolation)
        await _userRepo.Received(1).FindByIdWithRolesAsync(
            userId, tenantId, Arg.Any<CancellationToken>());
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
        FullName = "Full Name Test",
        PasswordHash = "hash",
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
        IsSystem = false,
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
        Module = "tramites",
        Action = "create",
        RolePermissions = []
    };
}
