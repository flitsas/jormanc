using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Interfaces;

namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Handler de ListRolesQuery.
/// Devuelve únicamente roles del tenant extraído del JWT (AC2 HU-9770).
/// </summary>
public sealed class ListRolesQueryHandler(IRoleRepository roleRepository)
{
    public async Task<RoleDto[]> HandleAsync(
        ListRolesQuery query, CancellationToken ct = default)
    {
        var roles = await roleRepository.ListByTenantAsync(query.TenantId, ct);

        return roles.Select(r => new RoleDto(
            Id: r.Id,
            Slug: r.Slug,
            Name: r.Name,
            Description: r.Description,
            IsSystem: r.IsSystem,
            Permissions: r.RolePermissions
                .Select(rp => rp.Permission.Slug)
                .ToArray()
        )).ToArray();
    }
}
