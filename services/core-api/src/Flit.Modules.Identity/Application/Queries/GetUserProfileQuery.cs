namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Query: obtener perfil del usuario autenticado. Usado por GET /auth/me.
/// AC3 de HU-9769.
/// </summary>
public sealed record GetUserProfileQuery(Guid UserId, Guid TenantId);
