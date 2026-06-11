namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Asociación tipo_trámite → documento requerido, con orden y obligatoriedad.
/// schema: documents / tabla: procedure_type_documents
/// </summary>
public sealed class ProcedureTypeDocument
{
    public Guid Id { get; set; }
    /// <summary>FK a procedures_config.procedure_types.</summary>
    public Guid ProcedureTypeId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Guid TenantId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int OrderIndex { get; set; }
    /// <summary>FK a procedures_config.actor_definitions (nullable).</summary>
    public Guid? ActorDefinitionId { get; set; }
    public bool AllowPartialConsolidation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
}
