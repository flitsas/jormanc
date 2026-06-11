using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Companies.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Companies.Infrastructure.Persistence;

/// <summary>
/// Implementación de ITenantService que escribe directamente en identity.tenants
/// vía el FlitDbContext compartido (modular monolith — mismo proceso, misma BD).
/// El módulo Companies no importa Flit.Modules.Identity para mantener el desacoplamiento.
/// </summary>
public sealed class TenantService(FlitDbContext db) : ITenantService
{
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public async Task<Guid> CreateTenantAsync(string slug, string name, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        return tenant.Id;
    }
}
