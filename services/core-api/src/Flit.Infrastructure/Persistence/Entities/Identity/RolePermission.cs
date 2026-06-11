namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Asignación rol → permiso. PK compuesta (role_id, permission_id).
/// schema: identity / tabla: role_permissions
/// </summary>
#pragma warning disable CA1711
public sealed class RolePermission
#pragma warning restore CA1711
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
