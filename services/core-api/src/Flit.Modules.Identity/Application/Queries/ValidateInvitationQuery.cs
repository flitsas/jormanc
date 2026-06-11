namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Query: validar token de invitación antes de mostrar el formulario de registro.
/// AC2 de HU-9772: GET /invitations/{token}/validate
/// </summary>
public sealed record ValidateInvitationQuery(string Token);
