using Flit.Infrastructure.Persistence.Entities.Integrations;

namespace Flit.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repositorio de configuraciones de conectores por tenant.
/// Los registros en connector_configs determinan el proveedor primario y secundario
/// que el ConnectorRouter usa para hot-failover (ADR-0011).
/// </summary>
public interface IConnectorConfigRepository
{
    /// <summary>
    /// Retorna los conectores activos para el tenant en orden de prioridad ascendente
    /// (prioridad 1 = primario).
    /// </summary>
    Task<IReadOnlyList<ConnectorConfig>> GetActiveByTenantAndTypeAsync(
        Guid tenantId, string connectorType, CancellationToken ct = default);

    /// <summary>Retorna el conector primario (is_primary = true) para el tenant.</summary>
    Task<ConnectorConfig?> GetPrimaryAsync(
        Guid tenantId, string connectorType, CancellationToken ct = default);

    /// <summary>Crea o actualiza configuración de conector para el tenant.</summary>
    Task UpsertAsync(ConnectorConfig config, CancellationToken ct = default);

    /// <summary>Lista todos los conectores de un tenant (todas las instancias activas e inactivas).</summary>
    Task<IReadOnlyList<ConnectorConfig>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct = default);
}
