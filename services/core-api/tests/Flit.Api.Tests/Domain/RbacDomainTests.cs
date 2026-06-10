using FluentAssertions;
using Flit.Modules.Rbac.Domain;
using Xunit;

namespace Flit.Api.Tests.Domain;

/// <summary>Unit tests del aggregate Role (ADR-0011).</summary>
public class RoleDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_code_to_uppercase_and_trims()
    {
        var role = Role.Create("  procedures_operator ", "Operador", null, false, Now);
        role.Code.Should().Be("PROCEDURES_OPERATOR");
        role.IsActive.Should().BeTrue();
        role.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void System_role_cannot_be_renamed()
    {
        var role = Role.Create("ADMIN", "Administrador", null, isSystem: true, Now);
        Action act = () => role.UpdateProfile("Otro nombre", null, Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*sistema*");
    }

    [Fact]
    public void Admin_role_cannot_be_deactivated()
    {
        var role = Role.Create("ADMIN", "Administrador", null, isSystem: true, Now);
        Action act = () => role.Deactivate(Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*ADMIN*");
    }

    [Fact]
    public void Non_admin_system_role_can_be_deactivated()
    {
        var role = Role.Create("AUDITOR", "Auditor", null, isSystem: true, Now);
        role.Deactivate(Now);
        role.IsActive.Should().BeFalse();
    }
}

/// <summary>Unit tests del aggregate Permission.</summary>
public class PermissionDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_validates_module_action_format()
    {
        Action act = () => Permission.Create("SOLOACCION", "Algo", null, "MODULE", false, Now);
        act.Should().Throw<ArgumentException>().WithMessage("*MODULE.ACTION*");
    }

    [Fact]
    public void Create_accepts_dotted_codes_and_uppercases()
    {
        var p = Permission.Create("procedures.list", "Listar", null, "procedures", false, Now);
        p.Code.Should().Be("PROCEDURES.LIST");
        p.Module.Should().Be("PROCEDURES");
    }
}

/// <summary>Unit tests del aggregate MenuItem.</summary>
public class MenuItemDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_code_uppercase()
    {
        var mi = MenuItem.Create("dashboard", null, "Dashboard", "layout-dashboard",
            "/dashboard", sortOrder: 1, isSeparator: false, Now);
        mi.Code.Should().Be("DASHBOARD");
        mi.IsActive.Should().BeTrue();
        mi.IsVisible.Should().BeTrue();
        mi.IsSeparator.Should().BeFalse();
    }

    [Fact]
    public void Create_accepts_parent_for_nested_items()
    {
        var parent = MenuItem.Create("PROCEDURES", null, "Trámites", "file-text", null, 2, false, Now);
        var child = MenuItem.Create("PROCEDURES_LIST", parent.Id, "Listar", "list",
            "/procedures", 1, false, Now);

        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void Hide_Show_toggle_visibility()
    {
        var mi = MenuItem.Create("X", null, "X", null, null, 0, false, Now);
        mi.Hide();
        mi.IsVisible.Should().BeFalse();
        mi.Show();
        mi.IsVisible.Should().BeTrue();
    }
}
