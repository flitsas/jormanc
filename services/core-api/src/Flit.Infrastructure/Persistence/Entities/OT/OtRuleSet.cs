namespace Flit.Infrastructure.Persistence.Entities.OT;

/// <summary>
/// Regla dinámica del OT. Hot-swap sin despliegue — el motor recarga desde BD.
/// schema: ot / tabla: ot_rule_sets
/// </summary>
public sealed class OtRuleSet
{
    public Guid Id { get; set; }
    public Guid OtId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>JSONB — árbol AND/OR de condiciones (mismo formato que procedures_config.rule_sets).</summary>
    public string Conditions { get; set; } = "{}";
    /// <summary>JSONB — lista de acciones.</summary>
    public string Actions { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public OtOrganism OtOrganism { get; set; } = null!;
}
