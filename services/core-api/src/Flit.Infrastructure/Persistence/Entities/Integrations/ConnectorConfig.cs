namespace Flit.Infrastructure.Persistence.Entities.Integrations;

/// <summary>
/// Configuración del conector externo por tenant (RUNT/SIMIT/RUES/Quipux).
/// El proveedor primario se usa primero; si falla, se recurre al secundario (hot-failover, ADR-0011).
/// schema: integrations / tabla: connector_configs
/// </summary>
public sealed class ConnectorConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>runt | simit | rues | identity | quipux</summary>
    public string ConnectorType { get; set; } = string.Empty;
    /// <summary>verifik | intempo | native | mock</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>JSONB — referencia a credenciales (nunca texto plano).</summary>
    public string CredentialsRef { get; set; } = "{}";
    public bool IsPrimary { get; set; } = true;
    public int Priority { get; set; } = 1;
    public int TimeoutMs { get; set; } = 4000;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }
}
