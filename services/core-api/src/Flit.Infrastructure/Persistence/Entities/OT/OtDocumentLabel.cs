namespace Flit.Infrastructure.Persistence.Entities.OT;

/// <summary>
/// Etiqueta personalizada de documento por OT.
/// Eliminar requiere confirmación si hay adjuntos usando la etiqueta.
/// schema: ot / tabla: ot_document_labels
/// </summary>
public sealed class OtDocumentLabel
{
    public Guid Id { get; set; }
    public Guid OtId { get; set; }
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public OtOrganism OtOrganism { get; set; } = null!;
}
