namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Raíz del árbol multi-tenant. No tiene tenant_id ni created_by/updated_by (es la raíz).
/// schema: identity / tabla: tenants
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int RowVersion { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Invitation> Invitations { get; set; } = [];
}
