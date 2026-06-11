using Flit.Modules.Integrations.Application.DTOs;
using Flit.Modules.Integrations.Domain.Interfaces;

namespace Flit.Modules.Integrations.Application.Queries;

/// <summary>Handler de GetConnectorConfigQuery.</summary>
public sealed class GetConnectorConfigQueryHandler(IConnectorConfigRepository configRepository)
{
    public async Task<IReadOnlyList<ConnectorConfigDto>> HandleAsync(
        GetConnectorConfigQuery query, CancellationToken ct = default)
    {
        var configs = await configRepository.GetByTenantAsync(query.TenantId, ct);

        return configs.Select(c =>
            new ConnectorConfigDto(
                c.Id, c.TenantId, c.ConnectorType, c.Provider,
                c.IsPrimary, c.Priority, c.TimeoutMs, c.IsActive,
                c.CreatedAt, c.UpdatedAt))
            .ToList();
    }
}
