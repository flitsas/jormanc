namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>DTO de rol devuelto en GET /roles y POST /roles.</summary>
public sealed record RoleDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    bool IsSystem,
    string[] Permissions);

/// <summary>DTO mínimo de rol (id + slug + name) usado en PATCH /users/{id}/roles.</summary>
public sealed record RoleSummaryDto(
    Guid Id,
    string Slug,
    string Name);
