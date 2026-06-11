namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Paso del pipeline de un tipo de trámite.
/// schema: procedures_config / tabla: procedure_steps
/// </summary>
public sealed class ProcedureStep
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public Guid TenantId { get; set; }
    public int OrderIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>form | api_call | signature | review | identity_validation</summary>
    public string StepType { get; set; } = "form";
    public bool IsRequired { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
    public ICollection<FormSection> FormSections { get; set; } = [];
}
