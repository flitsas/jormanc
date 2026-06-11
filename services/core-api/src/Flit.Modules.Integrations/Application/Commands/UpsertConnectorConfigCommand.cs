namespace Flit.Modules.Integrations.Application.Commands;

/// <summary>
/// Crea o actualiza la configuración de un conector externo por tenant.
/// Si ya existe un registro para (tenant_id, connector_type, provider), lo actualiza.
/// AC1 HU-9775: el proveedor primario se configura aquí por tenant.
/// </summary>
public sealed record UpsertConnectorConfigCommand(
    Guid TenantId,
    /// <summary>runt | simit | rues | identity | quipux</summary>
    string ConnectorType,
    /// <summary>verifik | intempo | mock</summary>
    string Provider,
    bool IsPrimary,
    int Priority,
    int TimeoutMs,
    bool IsActive,
    Guid RequestedByUserId);
