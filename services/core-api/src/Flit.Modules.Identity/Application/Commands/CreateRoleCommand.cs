namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando para crear un nuevo rol en el tenant.
/// AC1 HU-9770: POST /api/v1/roles
/// </summary>
public sealed record CreateRoleCommand(
    Guid TenantId,
    Guid RequestedByUserId,
    string Slug,
    string Name,
    string? Description,
    IReadOnlyList<Guid> PermissionIds);
