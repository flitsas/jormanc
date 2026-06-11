using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio del catálogo global de permisos. HU-9770.
/// Los permisos son globales (no tienen tenant_id).
/// </summary>
public sealed class PermissionRepository(FlitDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken ct = default) =>
        await db.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Action)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IEnumerable<Guid> permissionIds, CancellationToken ct = default)
    {
        var ids = permissionIds.ToList();
        return await db.Permissions
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
    }
}
