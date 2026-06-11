namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Documento generado o cargado en un trámite.
/// schema: documents / tabla: procedure_documents
/// </summary>
public sealed class ProcedureDocument
{
    public Guid Id { get; set; }
    /// <summary>FK a procedures.procedures.</summary>
    public Guid ProcedureId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>Versión de plantilla con la que fue generado (nullable si origin='uploaded').</summary>
    public Guid? TemplateVersionId { get; set; }
    /// <summary>generated | uploaded</summary>
    public string Origin { get; set; } = "uploaded";
    /// <summary>pending | ready | failed | expired</summary>
    public string Status { get; set; } = "pending";
    /// <summary>Clave en MinIO.</summary>
    public string? FileRef { get; set; }
    public string? FileName { get; set; }
    /// <summary>JSONB — fuentes de datos usadas en la generación.</summary>
    public string? GenerationMetadata { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
}
