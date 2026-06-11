using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Integrations.Infrastructure.Persistence;

/// <summary>
/// Implementación EF Core del repositorio de connector_configs.
/// AC1 HU-9775: persiste el proveedor primario configurado por tenant.
/// </summary>
public sealed class ConnectorConfigRepository(FlitDbContext db) : IConnectorConfigRepository
{
    public Task<IReadOnlyList<ConnectorConfig>> GetActiveByTenantAndTypeAsync(
        Guid tenantId, string connectorType, CancellationToken ct = default) =>
        db.ConnectorConfigs
            .Where(c => c.TenantId == tenantId
                        && c.ConnectorType == connectorType
                        && c.IsActive)
            .OrderBy(c => c.Priority)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<ConnectorConfig>>(t => t.Result, ct);

    public Task<ConnectorConfig?> GetPrimaryAsync(
        Guid tenantId, string connectorType, CancellationToken ct = default) =>
        db.ConnectorConfigs
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId
                && c.ConnectorType == connectorType
                && c.IsPrimary
                && c.IsActive, ct);

    public async Task UpsertAsync(ConnectorConfig config, CancellationToken ct = default)
    {
        var existing = await db.ConnectorConfigs
            .FirstOrDefaultAsync(c =>
                c.TenantId == config.TenantId
                && c.ConnectorType == config.ConnectorType
                && c.Provider == config.Provider, ct);

        if (existing is null)
            db.ConnectorConfigs.Add(config);
        else
        {
            existing.IsPrimary = config.IsPrimary;
            existing.Priority = config.Priority;
            existing.TimeoutMs = config.TimeoutMs;
            existing.IsActive = config.IsActive;
            existing.UpdatedAt = config.UpdatedAt;
            existing.UpdatedBy = config.UpdatedBy;
            existing.RowVersion = config.RowVersion + 1;
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<ConnectorConfig>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        db.ConnectorConfigs
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.ConnectorType)
            .ThenBy(c => c.Priority)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<ConnectorConfig>>(t => t.Result, ct);
}
