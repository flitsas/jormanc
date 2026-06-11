namespace Flit.Infrastructure.Persistence.Entities.OT;

/// <summary>
/// Prelación documental por OT y tipo de trámite. UPSERT objetivo < 500ms.
/// ordered_document_type_ids es JSONB con array de UUIDs en orden de prelación.
/// schema: ot / tabla: ot_document_order
/// </summary>
public sealed class OtDocumentOrder
{
    public Guid Id { get; set; }
    public Guid OtId { get; set; }
    /// <summary>FK a procedures_config.procedure_types.</summary>
    public Guid ProcedureTypeId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>JSONB — array de UUIDs de document_types en orden de prelación.</summary>
    public string OrderedDocumentTypeIds { get; set; } = "[]";
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public OtOrganism OtOrganism { get; set; } = null!;
}
