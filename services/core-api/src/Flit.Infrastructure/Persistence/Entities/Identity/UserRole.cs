namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Asignación usuario → rol. PK compuesta (user_id, role_id).
/// schema: identity / tabla: user_roles
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
