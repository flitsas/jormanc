namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Catálogo global de permisos del sistema. No pertenece a ningún tenant.
/// schema: identity / tabla: permissions
/// </summary>
#pragma warning disable CA1711
public sealed class Permission
#pragma warning restore CA1711
{
    public Guid Id { get; set; }
    /// <summary>Ej: "tramites.create", "users.manage_roles"</summary>
    public string Slug { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
