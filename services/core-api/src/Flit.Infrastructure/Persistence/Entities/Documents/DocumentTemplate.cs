namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Plantilla versionada de documento (HTML con marcadores, almacenada en MinIO).
/// Solo una versión 'active' por document_type_id (índice único parcial).
/// schema: documents / tabla: document_templates
/// </summary>
public sealed class DocumentTemplate
{
    public Guid Id { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Guid TenantId { get; set; }
    public int Version { get; set; }
    /// <summary>Clave MinIO: templates/{tenant}/{doc_type}/{version}.html</summary>
    public string ContentRef { get; set; } = string.Empty;
    /// <summary>active | deprecated | archived</summary>
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int RowVersion { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
    public ICollection<TemplateField> Fields { get; set; } = [];
}
