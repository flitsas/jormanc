namespace Flit.Infrastructure.Persistence.Entities.OT;

/// <summary>
/// Organismo de Tránsito. Puede operar en modo Dashboard (nativo) o QX (Quipux).
/// quipux_config contiene webhook_token_hash — nunca en texto plano.
/// schema: ot / tabla: ot_organisms
/// </summary>
public sealed class OtOrganism
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>dashboard | qx</summary>
    public string Mode { get; set; } = "dashboard";
    public bool QuipuxEnabled { get; set; }
    /// <summary>JSONB — { endpoint, webhook_token_hash, auth_config }</summary>
    public string? QuipuxConfig { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public ICollection<OtDocumentOrder> DocumentOrders { get; set; } = [];
    public ICollection<OtDocumentLabel> DocumentLabels { get; set; } = [];
    public ICollection<OtRuleSet> RuleSets { get; set; } = [];
    public ICollection<OtIntegrationLog> IntegrationLogs { get; set; } = [];
}
