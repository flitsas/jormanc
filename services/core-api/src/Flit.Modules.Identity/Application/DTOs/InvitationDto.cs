namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>Resultado de POST /invitations: resumen de la invitación creada.</summary>
public sealed record InvitationDto(
    Guid Id,
    string Email,
    DateTimeOffset ExpiresAt,
    string Status);

/// <summary>Resultado de GET /invitations/{token}/validate.</summary>
public sealed record InvitationValidateDto(
    string Email,
    Guid TenantId,
    string TenantName,
    string[] RoleSlugs);
