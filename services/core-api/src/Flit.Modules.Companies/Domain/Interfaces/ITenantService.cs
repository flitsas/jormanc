namespace Flit.Modules.Companies.Domain.Interfaces;

/// <summary>
/// Contrato para crear tenants en identity.tenants desde el módulo Companies.
/// Desacoplamiento inter-módulo: Companies no importa directamente Flit.Modules.Identity.
/// La implementación usa FlitDbContext (shared) para escribir en identity.tenants.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Verifica si el slug ya está en uso en identity.tenants.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Crea un registro en identity.tenants y retorna su ID.
    /// </summary>
    Task<Guid> CreateTenantAsync(string slug, string name, CancellationToken ct = default);
}
