namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Usuario del sistema. Pertenece a un tenant.
/// schema: identity / tabla: users
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>Hash Argon2id del password.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>pending | active | suspended | deleted</summary>
    public string Status { get; set; } = "active";
    public bool MustResetPwd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
}
