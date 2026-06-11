namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando: crear invitación para onboarding de nuevo usuario.
/// AC1 de HU-9772: POST /invitations { email, roles[], tenant_id }
/// </summary>
public sealed record CreateInvitationCommand(
    string Email,
    Guid[] RoleIds,
    Guid TenantId,
    Guid InvitedBy);
