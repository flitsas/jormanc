namespace Flit.Modules.Identity.Domain.Errors;

/// <summary>
/// Error tipado del dominio de identidad. Usado con Result&lt;T, IdentityError&gt;.
/// </summary>
public sealed record IdentityError(string Code, string Message)
{
    public static readonly IdentityError InvalidCredentials =
        new("INVALID_CREDENTIALS", "Email o password incorrectos.");

    public static readonly IdentityError TenantNotFound =
        new("TENANT_NOT_FOUND", "Tenant no encontrado.");

    public static readonly IdentityError UserNotActive =
        new("USER_NOT_ACTIVE", "La cuenta de usuario no está activa.");

    public static readonly IdentityError UserNotFound =
        new("USER_NOT_FOUND", "Usuario no encontrado.");

    public static readonly IdentityError RoleNotFound =
        new("ROLE_NOT_FOUND", "Rol no encontrado.");

    public static readonly IdentityError RoleSlugAlreadyExists =
        new("ROLE_SLUG_ALREADY_EXISTS", "Ya existe un rol con ese slug en el tenant.");

    public static readonly IdentityError SystemRoleCannotBeDeleted =
        new("SYSTEM_ROLE_CANNOT_BE_DELETED", "Los roles de sistema no pueden ser eliminados.");

    // ── HU-9772: Invitaciones ────────────────────────────────────────────────
    public static readonly IdentityError InvitationNotFound =
        new("INVITATION_NOT_FOUND", "Invitación no encontrada.");

    public static readonly IdentityError InvitationExpired =
        new("INVITATION_EXPIRED", "La invitación ha expirado.");

    public static readonly IdentityError InvitationAlreadyUsed =
        new("INVITATION_ALREADY_USED", "La invitación ya fue utilizada.");

    public static readonly IdentityError EmailAlreadyRegistered =
        new("EMAIL_ALREADY_REGISTERED", "El correo ya está registrado en este tenant.");

    // ── HU-9772: Reset de contraseña ─────────────────────────────────────────
    public static readonly IdentityError ResetTokenNotFound =
        new("RESET_TOKEN_NOT_FOUND", "Token de reset no encontrado o inválido.");

    public static readonly IdentityError ResetTokenExpired =
        new("RESET_TOKEN_EXPIRED", "El token de reset ha expirado.");

    public static readonly IdentityError ResetTokenAlreadyUsed =
        new("RESET_TOKEN_ALREADY_USED", "El token de reset ya fue utilizado.");
}
