using FluentAssertions;
using Flit.Modules.Rbac.Adapters;
using Flit.Modules.Rbac.Application;
using Flit.Modules.Rbac.Domain;
using Flit.SharedKernel;
using Xunit;

namespace Flit.Api.Tests.Application;

/// <summary>
/// Unit tests del modulo RBAC (ADR-0011).
/// Cubren AssignRoleToUser, GetUserMenu (construccion de arbol),
/// GetUserPermissions (cache hit/miss, invalidacion).
/// </summary>
public class RbacUseCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);
    private static IClock Clock => new FixedClock(Now);

    // ─── AssignRoleToUser ─────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleToUser_OK_invalidates_cache()
    {
        var (roles, perms, _, assignments, cache) = MakeRbacStubs();
        var role = Role.Create("OPERATOR", "Operator", null, false, Now);
        await roles.AddAsync(role, TestContext.Current.CancellationToken);

        var userId = Guid.CreateVersion7();
        // Pre-cargar cache simulando lookup previo
        await cache.SetAsync(userId, ["OLD.PERM"], TestContext.Current.CancellationToken);

        var result = await AssignRoleToUser.HandleAsync(new AssignRoleToUser.Command(userId, role.Id, AssignedByUserId: null), roles, assignments, cache, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.RoleId.Should().Be(role.Id);
        // cache debe estar invalidada
        var cached = await cache.GetAsync(userId, TestContext.Current.CancellationToken);
        cached.Should().BeNull();
    }

    [Fact]
    public async Task AssignRoleToUser_RoleNotFound_returns_error()
    {
        var (roles, _, _, assignments, cache) = MakeRbacStubs();
        var result = await AssignRoleToUser.HandleAsync(new AssignRoleToUser.Command(Guid.CreateVersion7(), Guid.CreateVersion7(), null), roles, assignments, cache, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<AssignRoleToUser.AssignRoleError.RoleNotFound>();
    }

    [Fact]
    public async Task AssignRoleToUser_RoleInactive_returns_error()
    {
        var (roles, _, _, assignments, cache) = MakeRbacStubs();
        var role = Role.Create("STALE", "Stale", null, false, Now);
        role.Deactivate(Now);
        await roles.AddAsync(role, TestContext.Current.CancellationToken);

        var result = await AssignRoleToUser.HandleAsync(new AssignRoleToUser.Command(Guid.CreateVersion7(), role.Id, null), roles, assignments, cache, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<AssignRoleToUser.AssignRoleError.RoleInactive>();
    }

    // ─── GetUserMenu ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserMenu_builds_tree_correctly()
    {
        var (roles, _, menuItems, assignments, _) = MakeRbacStubs();
        var role = Role.Create("ADMIN", "Admin", null, true, Now);
        await roles.AddAsync(role, TestContext.Current.CancellationToken);

        // Construye arbol: Dashboard (root), Procedures (root) > Procedures.List (child)
        var dashboard = MenuItem.Create("DASHBOARD", null, "Dashboard", "icon-dash", "/dashboard", 1, false, Now);
        var procedures = MenuItem.Create("PROCEDURES", null, "Trámites", "icon-doc", null, 2, false, Now);
        var procList = MenuItem.Create("PROCEDURES.LIST", procedures.Id, "Listar", "icon-list", "/procedures", 1, false, Now);
        await menuItems.AddAsync(dashboard, TestContext.Current.CancellationToken);
        await menuItems.AddAsync(procedures, TestContext.Current.CancellationToken);
        await menuItems.AddAsync(procList, TestContext.Current.CancellationToken);

        var userId = Guid.CreateVersion7();
        await assignments.AssignRoleToUserAsync(userId, role.Id, null, TestContext.Current.CancellationToken);
        await assignments.AssignMenuItemToRoleAsync(role.Id, dashboard.Id, TestContext.Current.CancellationToken);
        await assignments.AssignMenuItemToRoleAsync(role.Id, procedures.Id, TestContext.Current.CancellationToken);
        await assignments.AssignMenuItemToRoleAsync(role.Id, procList.Id, TestContext.Current.CancellationToken);

        var response = await GetUserMenu.HandleAsync(new GetUserMenu.Query(userId), menuItems, TestContext.Current.CancellationToken);

        response.Menu.Should().HaveCount(2); // 2 roots
        var dashNode = response.Menu.First(n => n.Code == "DASHBOARD");
        dashNode.Children.Should().BeEmpty();
        var procNode = response.Menu.First(n => n.Code == "PROCEDURES");
        procNode.Children.Should().HaveCount(1);
        procNode.Children[0].Code.Should().Be("PROCEDURES.LIST");
    }

    [Fact]
    public async Task GetUserMenu_returns_empty_when_user_has_no_role()
    {
        var (_, _, menuItems, _, _) = MakeRbacStubs();
        var response = await GetUserMenu.HandleAsync(new GetUserMenu.Query(Guid.CreateVersion7()), menuItems, TestContext.Current.CancellationToken);
        response.Menu.Should().BeEmpty();
    }

    // ─── GetUserPermissions ───────────────────────────────────────────

    [Fact]
    public async Task GetUserPermissions_returns_cached_when_present()
    {
        var (_, perms, _, _, cache) = MakeRbacStubs();
        var userId = Guid.CreateVersion7();
        var cachedCodes = new[] { "X.A", "X.B" };
        await cache.SetAsync(userId, cachedCodes, TestContext.Current.CancellationToken);

        var response = await GetUserPermissions.HandleAsync(new GetUserPermissions.Query(userId), perms, cache, TestContext.Current.CancellationToken);

        response.Permissions.Should().BeEquivalentTo(cachedCodes);
    }

    [Fact]
    public async Task GetUserPermissions_cache_miss_calls_repo_and_caches()
    {
        var (roles, perms, _, assignments, cache) = MakeRbacStubs();
        var role = Role.Create("OPERATOR", "Op", null, false, Now);
        await roles.AddAsync(role, TestContext.Current.CancellationToken);
        var perm = Permission.Create("PROCEDURES.LIST", "List", null, "PROCEDURES", true, Now);
        await perms.AddAsync(perm, TestContext.Current.CancellationToken);

        var userId = Guid.CreateVersion7();
        await assignments.AssignRoleToUserAsync(userId, role.Id, null, TestContext.Current.CancellationToken);
        await assignments.AssignPermissionToRoleAsync(role.Id, perm.Id, null, TestContext.Current.CancellationToken);

        var response1 = await GetUserPermissions.HandleAsync(new GetUserPermissions.Query(userId), perms, cache, TestContext.Current.CancellationToken);

        response1.Permissions.Should().Contain("PROCEDURES.LIST");

        // Segunda llamada debe devolver lo mismo (cache hit interno).
        var cached = await cache.GetAsync(userId, TestContext.Current.CancellationToken);
        cached.Should().NotBeNull();
        cached!.Should().Contain("PROCEDURES.LIST");
    }

    [Fact]
    public async Task UserHasPermissionAsync_checks_specific_code()
    {
        var (roles, perms, _, assignments, cache) = MakeRbacStubs();
        var role = Role.Create("SUPERVISOR", "Sup", null, false, Now);
        await roles.AddAsync(role, TestContext.Current.CancellationToken);
        var p = Permission.Create("PROCEDURES.APPROVE", "Approve", null, "PROCEDURES", true, Now);
        await perms.AddAsync(p, TestContext.Current.CancellationToken);

        var userId = Guid.CreateVersion7();
        await assignments.AssignRoleToUserAsync(userId, role.Id, null, TestContext.Current.CancellationToken);
        await assignments.AssignPermissionToRoleAsync(role.Id, p.Id, null, TestContext.Current.CancellationToken);

        var hasIt = await GetUserPermissions.UserHasPermissionAsync(userId, "PROCEDURES.APPROVE", perms, cache, TestContext.Current.CancellationToken);
        hasIt.Should().BeTrue();

        var hasOther = await GetUserPermissions.UserHasPermissionAsync(userId, "USERS.DELETE", perms, cache, TestContext.Current.CancellationToken);
        hasOther.Should().BeFalse();
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static (
        InMemoryRolesRepository roles,
        InMemoryPermissionsRepository perms,
        InMemoryMenuItemsRepository menuItems,
        InMemoryRoleAssignmentsRepository assignments,
        InMemoryPermissionsCache cache
    ) MakeRbacStubs()
    {
        var roles = new InMemoryRolesRepository();
        var assignments = new InMemoryRoleAssignmentsRepository();
        var perms = new InMemoryPermissionsRepository(assignments, roles);
        var menuItems = new InMemoryMenuItemsRepository(assignments);
        var cache = new InMemoryPermissionsCache();
        return (roles, perms, menuItems, assignments, cache);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
