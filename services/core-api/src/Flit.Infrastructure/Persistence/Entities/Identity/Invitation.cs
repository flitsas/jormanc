namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Invitación para onboarding de usuario. Válida 72h.
/// schema: identity / tabla: invitations
/// </summary>
public sealed class Invitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    /// <summary>SHA-256 del token enviado al usuario.</summary>
    public string TokenHash { get; set; } = string.Empty;
    /// <summary>Roles a asignar al aceptar (JSON array de UUIDs).</summary>
    public string RolesJson { get; set; } = "[]";
    /// <summary>pending | accepted | expired | cancelled</summary>
    public string Status { get; set; } = "pending";
    public Guid? InvitedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
