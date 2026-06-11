namespace Flit.Modules.Integrations.Domain.Errors;

/// <summary>
/// Errores de dominio del módulo Integrations.
/// Sigue Result pattern (ADR-0002 §8.1): errores de negocio, NO excepciones de infraestructura.
/// </summary>
public sealed record IntegrationsError(string Code, string Message)
{
    public static readonly IntegrationsError ConnectorConfigNotFound =
        new("INTEGRATIONS_CONNECTOR_CONFIG_NOT_FOUND",
            "No se encontró configuración de conector para este tenant.");

    public static readonly IntegrationsError AllConnectorsFailed =
        new("INTEGRATIONS_ALL_CONNECTORS_FAILED",
            "Todos los proveedores RUNT fallaron (timeout o error de servidor). Intente más tarde.");

    public static readonly IntegrationsError InvalidProvider =
        new("INTEGRATIONS_INVALID_PROVIDER",
            "Proveedor inválido. Valores permitidos: verifik, intempo, mock.");

    public static readonly IntegrationsError InvalidConnectorType =
        new("INTEGRATIONS_INVALID_CONNECTOR_TYPE",
            "Tipo de conector inválido. Valores permitidos: runt, simit, rues, identity, quipux.");

    public static readonly IntegrationsError TenantRequired =
        new("INTEGRATIONS_TENANT_REQUIRED", "Se requiere tenant_id para ejecutar la consulta.");
}
