namespace Flit.Modules.Integrations.Application.Queries;

/// <summary>
/// Consulta la configuración de conectores activos para un tenant.
/// Usada por el endpoint GET /companies/{id}/config/connector (SuperAdmin).
/// </summary>
public sealed record GetConnectorConfigQuery(Guid TenantId);
