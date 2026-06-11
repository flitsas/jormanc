using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Api.HostedServices;

/// <summary>
/// Seed de desarrollo: crea un tenant de prueba, un usuario admin y permisos básicos
/// si no existen. Solo activo en Development (comprobado al iniciar).
/// Usuario: admin@acme.com / Flit2026@Dev! · Tenant slug: acme
/// </summary>
public sealed class DevSeedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment env,
    IPasswordHasher passwordHasher,
    ILogger<DevSeedService> logger) : IHostedService
{
    private static readonly Guid SystemSeedId = new("00000000-0000-0000-0000-000000000001");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlitDbContext>();

        await SeedAsync(db, cancellationToken);
    }

    private async Task SeedAsync(FlitDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Tenant
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "acme", ct);
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Slug = "acme",
                Name = "Acme Corp",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev seed: Tenant 'acme' creado ({Id})", tenant.Id);
        }

        // Permissions
        var permSlugs = new[]
        {
            ("users.read", "users", "read", "Listar usuarios"),
            ("users.create", "users", "create", "Crear usuarios"),
            ("users.update", "users", "update", "Actualizar usuarios"),
            ("users.delete", "users", "delete", "Eliminar usuarios"),
            ("roles.read", "roles", "read", "Listar roles"),
            ("tramites.create", "tramites", "create", "Crear trámites"),
            ("tramites.read", "tramites", "read", "Ver trámites"),
            ("analytics.read", "analytics", "read", "Ver dashboard analítico"),
            ("analytics.export", "analytics", "export", "Exportar dashboard Excel/PDF")
        };

        var existingPerms = await db.Permissions.ToListAsync(ct);
        var existingPermSlugs = existingPerms.Select(p => p.Slug).ToHashSet();

        foreach (var (slug, module, action, desc) in permSlugs)
        {
            if (!existingPermSlugs.Contains(slug))
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Slug = slug,
                    Module = module,
                    Action = action,
                    Description = desc
                });
            }
        }
        await db.SaveChangesAsync(ct);

        // Role admin
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Slug == "admin" && r.TenantId == tenant.Id, ct);
        if (role is null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Slug = "admin",
                Name = "Administrador",
                Description = "Rol de administrador del tenant",
                IsSystem = true,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev seed: Role 'admin' creado ({Id})", role.Id);
        }

        // RolePermissions (admin gets all permissions)
        var allPerms = await db.Permissions.ToListAsync(ct);
        var existingRolePerms = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(ct);

        foreach (var perm in allPerms)
        {
            if (!existingRolePerms.Contains(perm.Id))
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }
        }
        await db.SaveChangesAsync(ct);

        // User
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "admin@acme.com" && u.TenantId == tenant.Id, ct);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = "admin@acme.com",
                FullName = "Admin FLIT Dev",
                PasswordHash = passwordHasher.Hash("Flit2026@Dev!"),
                Status = "active",
                MustResetPwd = false,
                CreatedAt = now,
                CreatedBy = SystemSeedId,
                UpdatedAt = now,
                UpdatedBy = SystemSeedId
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev seed: Usuario 'admin@acme.com' creado ({Id})", user.Id);
        }

        // UserRole
        var hasRole = await db.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);

        if (!hasRole)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = tenant.Id,
                AssignedAt = now,
                AssignedBy = SystemSeedId
            });
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Dev seed completo. Login: admin@acme.com / Flit2026@Dev! · tenant_slug: acme");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
