using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio de roles para el módulo Identity (HU-9770).
/// </summary>
public interface IRoleRepository
{
    /// <summary>Lista todos los roles activos del tenant.</summary>
    Task<IReadOnlyList<Role>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Busca un rol por id y tenant.</summary>
    Task<Role?> FindByIdAsync(Guid roleId, Guid tenantId, CancellationToken ct = default);

    /// <summary>Verifica si ya existe un rol con ese slug en el tenant.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid tenantId, CancellationToken ct = default);

    /// <summary>Persiste un nuevo rol.</summary>
    Task CreateAsync(Role role, CancellationToken ct = default);

    /// <summary>Soft-delete del rol (establece DeletedAt).</summary>
    Task DeleteAsync(Role role, CancellationToken ct = default);

    /// <summary>Retorna los roles con permisos incluidos para los ids dados dentro del tenant.</summary>
    Task<IReadOnlyList<Role>> GetByIdsAsync(
        IEnumerable<Guid> roleIds, Guid tenantId, CancellationToken ct = default);
}
