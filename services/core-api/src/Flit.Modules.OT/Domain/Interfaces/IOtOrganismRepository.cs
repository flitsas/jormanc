using Flit.Infrastructure.Persistence.Entities.OT;

namespace Flit.Modules.OT.Domain.Interfaces;

public interface IOtOrganismRepository
{
    Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default);

    Task<IReadOnlyList<OtOrganism>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);

    Task<OtOrganism?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<OtOrganism?> FindBySlugAsync(string slug, CancellationToken ct = default);

    Task CreateAsync(OtOrganism organism, CancellationToken ct = default);

    Task UpdateAsync(OtOrganism organism, CancellationToken ct = default);

    Task SoftDeleteAsync(OtOrganism organism, Guid deletedBy, DateTimeOffset deletedAt, CancellationToken ct = default);
}
