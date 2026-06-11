namespace Flit.Modules.Integrations.Application.Queries;

/// <summary>
/// Consulta logs de integración paginados por tenant (AC3 HU-9775).
/// Solo SuperAdmin puede ejecutar esta query.
/// </summary>
public sealed record GetIntegrationLogsQuery(
    Guid TenantId,
    int Page,
    int PageSize,
    string? ConnectorType,
    string? Provider,
    DateTimeOffset? From,
    DateTimeOffset? To);
