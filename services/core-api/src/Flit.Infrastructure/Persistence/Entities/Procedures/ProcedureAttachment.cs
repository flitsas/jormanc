namespace Flit.Infrastructure.Persistence.Entities.Procedures;

/// <summary>
/// Adjunto subido a un trámite, clasificado por etiqueta dinámica del OT.
/// schema: procedures / tabla: procedure_attachments
/// </summary>
public sealed class ProcedureAttachment
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>Etiqueta dinámica del OT (ej. "carta_poder", "cedula_representante").</summary>
    public string LabelSlug { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    /// <summary>Clave en MinIO.</summary>
    public string FileRef { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public Procedure Procedure { get; set; } = null!;
}
