namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Regla de consulta parametrizable por actor (RUNT, SIMIT, RUES, liveness).
/// schema: procedures_config / tabla: query_rules
/// </summary>
public sealed class QueryRule
{
    public Guid Id { get; set; }
    public Guid ActorDefinitionId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>persona_natural | persona_juridica | representante_legal | vehiculo</summary>
    public string SubjectType { get; set; } = string.Empty;
    /// <summary>document_number | nit | placa | vin</summary>
    public string EntryKey { get; set; } = string.Empty;
    public bool IsBlocking { get; set; } = true;
    /// <summary>JSONB — lista de verificaciones: [{ type, is_active, order }]</summary>
    public string Verifications { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ActorDefinition ActorDefinition { get; set; } = null!;
}
