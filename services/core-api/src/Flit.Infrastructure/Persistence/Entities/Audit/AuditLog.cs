namespace Flit.Infrastructure.Persistence.Entities.Audit;

/// <summary>
/// Bitácora transversal inmutable de cambios en tablas de negocio.
/// Llenado por trigger genérico. No tiene soft delete ni row_version.
/// schema: audit / tabla: audit_log
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    /// <summary>I = Insert, U = Update, D = Delete</summary>
    public char Operation { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    /// <summary>JSONB — valores antes del cambio.</summary>
    public string? OldValues { get; set; }
    /// <summary>JSONB — valores después del cambio.</summary>
    public string? NewValues { get; set; }
    public Guid? RequestId { get; set; }
    public string? IpAddress { get; set; }
}
