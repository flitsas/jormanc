using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Interfaces;

namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Handler de ListPermissionsQuery. Catálogo global de permisos.
/// </summary>
public sealed class ListPermissionsQueryHandler(IPermissionRepository permissionRepository)
{
    public async Task<PermissionDto[]> HandleAsync(
        ListPermissionsQuery query, CancellationToken ct = default)
    {
        var permissions = await permissionRepository.ListAllAsync(ct);

        return permissions.Select(p => new PermissionDto(
            Id: p.Id,
            Slug: p.Slug,
            Module: p.Module,
            Action: p.Action,
            Description: p.Description
        )).ToArray();
    }
}
