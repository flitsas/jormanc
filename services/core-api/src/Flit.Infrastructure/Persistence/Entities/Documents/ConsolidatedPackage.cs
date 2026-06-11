namespace Flit.Infrastructure.Persistence.Entities.Documents;

/// <summary>
/// Paquete PDF consolidado (merge de N documentos). Versionado: anterior queda en MinIO.
/// schema: documents / tabla: consolidated_packages
/// </summary>
public sealed class ConsolidatedPackage
{
    public Guid Id { get; set; }
    /// <summary>FK a procedures.procedures.</summary>
    public Guid ProcedureId { get; set; }
    public Guid TenantId { get; set; }
    public int Version { get; set; }
    /// <summary>Clave MinIO del PDF consolidado.</summary>
    public string MergedFileRef { get; set; } = string.Empty;
    /// <summary>Ej: "TRAMITE_{id}_{tipo}_{fecha}.pdf"</summary>
    public string DownloadFilename { get; set; } = string.Empty;
    public int DocCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}
