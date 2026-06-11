namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Snapshot inmutable de la configuración de un tipo de trámite al radicar.
/// Garantiza que trámites radicados no se afectan por cambios posteriores de parametrización.
/// schema: procedures_config / tabla: procedure_type_snapshots
/// </summary>
public sealed class ProcedureTypeSnapshot
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public int Version { get; set; }
    /// <summary>JSONB — copia completa de steps→sections→fields + rules + actors.</summary>
    public string SnapshotJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
}
