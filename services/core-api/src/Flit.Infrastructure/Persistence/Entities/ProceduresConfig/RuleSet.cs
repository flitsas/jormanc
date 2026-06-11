namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Regla de negocio de un tipo de trámite. conditions y actions son árboles AND/OR (ADR-0009).
/// schema: procedures_config / tabla: rule_sets
/// </summary>
public sealed class RuleSet
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>JSONB — árbol AND/OR de condiciones.</summary>
    public string Conditions { get; set; } = "{}";
    /// <summary>JSONB — lista de acciones UI.</summary>
    public string Actions { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
}
