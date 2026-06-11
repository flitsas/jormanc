using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio de roles usando FlitDbContext (EF Core). HU-9770.
/// Aplica filtro por tenant_id en todas las consultas (aislamiento multi-tenant AC2).
/// </summary>
public sealed class RoleRepository(FlitDbContext db) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> ListByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public Task<Role?> FindByIdAsync(
        Guid roleId, Guid tenantId, CancellationToken ct = default) =>
        db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.Id == roleId && r.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

    public Task<bool> SlugExistsAsync(
        string slug, Guid tenantId, CancellationToken ct = default) =>
        db.Roles
            .AnyAsync(r => r.Slug == slug && r.TenantId == tenantId, ct);

    public async Task CreateAsync(Role role, CancellationToken ct = default)
    {
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Role role, CancellationToken ct = default)
    {
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(
        IEnumerable<Guid> roleIds, Guid tenantId, CancellationToken ct = default)
    {
        var ids = roleIds.ToList();
        return await db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => ids.Contains(r.Id) && r.TenantId == tenantId)
            .ToListAsync(ct);
    }
}
