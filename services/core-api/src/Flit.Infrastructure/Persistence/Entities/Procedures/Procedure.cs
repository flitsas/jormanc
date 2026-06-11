namespace Flit.Infrastructure.Persistence.Entities.Procedures;

/// <summary>
/// Trámite en ejecución (runtime). Contiene snapshot de la config al radicar.
/// schema: procedures / tabla: procedures
/// </summary>
public sealed class Procedure
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>FK a ot.ot_organisms (nullable).</summary>
    public Guid? OtId { get; set; }
    public Guid ProcedureTypeId { get; set; }
    /// <summary>Snapshot inmutable de la config con la que inició el trámite.</summary>
    public Guid ProcedureTypeSnapshotId { get; set; }
    /// <summary>ID compuesto: "TRASP-02_EVE-8841"</summary>
    public string CompositeId { get; set; } = string.Empty;
    /// <summary>draft | submitted | processing_documents | pending_signatures | approved | rejected | cancelled</summary>
    public string Status { get; set; } = "draft";
    public int CurrentStepOrder { get; set; } = 1;
    /// <summary>JSONB — datos capturados por paso.</summary>
    public string StepData { get; set; } = "{}";
    public Guid? AssignedUserId { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public ICollection<ProcedureActor> Actors { get; set; } = [];
    public ICollection<VehicleQuery> VehicleQueries { get; set; } = [];
    public ICollection<ProcedureSignature> Signatures { get; set; } = [];
    public ICollection<ProcedureAttachment> Attachments { get; set; } = [];
}
