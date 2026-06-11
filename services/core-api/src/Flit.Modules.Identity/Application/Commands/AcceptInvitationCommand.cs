namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando: aceptar invitación y crear usuario activo.
/// AC1 de HU-9772: POST /invitations/{token}/accept { full_name, password }
/// </summary>
public sealed record AcceptInvitationCommand(
    string Token,
    string FullName,
    string Password);
