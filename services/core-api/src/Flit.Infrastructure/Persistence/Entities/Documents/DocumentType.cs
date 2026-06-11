namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Catálogo de tipos de documento por tenant.
/// schema: documents / tabla: document_types
/// </summary>
public sealed class DocumentType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>carga | generacion</summary>
    public string LoadType { get; set; } = "carga";
    /// <summary>JSONB — formatos permitidos: ["pdf", "jpg", "png"]</summary>
    public string AllowedFormats { get; set; } = """["pdf"]""";
    public int MaxSizeMb { get; set; } = 10;
    public bool IsReusable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public ICollection<DocumentTemplate> Templates { get; set; } = [];
    public ICollection<ProcedureTypeDocument> ProcedureTypeDocuments { get; set; } = [];
}
