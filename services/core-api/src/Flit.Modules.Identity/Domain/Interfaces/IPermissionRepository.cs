using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio del catálogo global de permisos (HU-9770).
/// Los permisos son globales (no por tenant).
/// </summary>
public interface IPermissionRepository
{
    /// <summary>Lista todos los permisos del catálogo del sistema.</summary>
    Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken ct = default);

    /// <summary>Retorna permisos por sus ids.</summary>
    Task<IReadOnlyList<Permission>> GetByIdsAsync(
        IEnumerable<Guid> permissionIds, CancellationToken ct = default);
}
