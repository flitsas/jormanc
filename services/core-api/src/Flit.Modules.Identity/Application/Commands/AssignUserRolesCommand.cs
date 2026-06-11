namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando para asignar/reemplazar los roles de un usuario.
/// AC1 HU-9770: PATCH /api/v1/users/{userId}/roles
/// Revoca todas las sesiones activas del usuario tras el cambio.
/// </summary>
public sealed record AssignUserRolesCommand(
    Guid UserId,
    Guid TenantId,
    Guid RequestedByUserId,
    IReadOnlyList<Guid> RoleIds);
