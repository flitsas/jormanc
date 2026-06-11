using Flit.Infrastructure.Persistence.Entities.Integrations;

namespace Flit.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Repositorio de logs de integración (inmutables — solo INSERT y consulta).
/// AC3 HU-9775: cada llamada registra payload en integrations.integration_logs por tenant.
/// </summary>
public interface IIntegrationLogRepository
{
    /// <summary>
    /// Persiste un registro de log. No lanzar excepción si falla (logging best-effort).
    /// </summary>
    Task AddAsync(IntegrationLog log, CancellationToken ct = default);

    /// <summary>
    /// Retorna logs paginados filtrados por tenant, tipo de conector y rango de fechas.
    /// </summary>
    Task<(IReadOnlyList<IntegrationLog> Items, int Total)> ListAsync(
        IntegrationLogFilter filter, CancellationToken ct = default);
}

/// <summary>Filtros para listado de integration_logs.</summary>
public sealed record IntegrationLogFilter(
    Guid TenantId,
    int Page,
    int PageSize,
    string? ConnectorType,
    string? Provider,
    DateTimeOffset? From,
    DateTimeOffset? To);
