namespace Flit.Infrastructure.Persistence.Entities.Identity;

/// <summary>
/// Token temporal para reset de contraseña.
/// schema: identity / tabla: password_reset_tokens
/// </summary>
public sealed class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>SHA-256 del token enviado al usuario.</summary>
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
