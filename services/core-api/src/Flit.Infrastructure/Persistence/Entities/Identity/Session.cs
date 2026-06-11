namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Sesión de usuario. Usada para blacklist de JTIs via IMemoryCache (ADR-0013).
/// schema: identity / tabla: sessions
/// </summary>
public sealed class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>JWT ID — clave de blacklist en IMemoryCache.</summary>
    public string Jti { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
