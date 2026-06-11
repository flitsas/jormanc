namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando para eliminar (soft-delete) un rol del tenant.
/// AC3 HU-9770: DELETE /api/v1/roles/{id} — is_system=true → 409
/// </summary>
public sealed record DeleteRoleCommand(
    Guid RoleId,
    Guid TenantId,
    Guid RequestedByUserId);
