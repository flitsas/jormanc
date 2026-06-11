namespace Flit.Infrastructure.Persistence.Entities.Procedures;

/// <summary>
/// Firma requerida/realizada en un trámite.
/// schema: procedures / tabla: procedure_signatures
/// </summary>
public sealed class ProcedureSignature
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>identidad_digital | firma_pantalla | preasignada</summary>
    public string SignatureType { get; set; } = string.Empty;
    /// <summary>pending | signed | rejected | expired</summary>
    public string Status { get; set; } = "pending";
    /// <summary>Referencia al archivo de firma en MinIO.</summary>
    public string? FileRef { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public Procedure Procedure { get; set; } = null!;
    public ProcedureActor Actor { get; set; } = null!;
}
