using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Repositorio de usuarios usando FlitDbContext (EF Core).
/// Login usa IgnoreQueryFilters para incluir usuarios cuyo tenant
/// podría estar filtrado; la verificación de status se hace en el handler.
/// </summary>
public sealed class UserRepository(FlitDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAndTenantSlugAsync(
        string email, string tenantSlug, CancellationToken ct = default) =>
        db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(u => u.Email == email && u.Tenant.Slug == tenantSlug)
            .FirstOrDefaultAsync(ct);

    public Task<User?> FindByIdWithRolesAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default) =>
        db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

    // ── HU-9772 ──────────────────────────────────────────────────────────────

    public Task<bool> EmailExistsAsync(
        string email, Guid tenantId, CancellationToken ct = default) =>
        db.Users
            .AnyAsync(u => u.Email == email && u.TenantId == tenantId, ct);

    public Task<User?> FindByEmailAndTenantIdAsync(
        string email, Guid tenantId, CancellationToken ct = default) =>
        db.Users
            .Include(u => u.Tenant)
            .Where(u => u.Email == email && u.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task<(IReadOnlyList<User> Items, int Total)> ListByTenantPaginatedAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
    {
        var query = db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, $"%{term}%") ||
                EF.Functions.ILike(u.FullName, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
