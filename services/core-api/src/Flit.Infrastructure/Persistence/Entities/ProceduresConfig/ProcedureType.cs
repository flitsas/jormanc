namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Tipo de trámite parametrizable (low-code). Versiona con cada cambio.
/// schema: procedures_config / tabla: procedure_types
/// </summary>
public sealed class ProcedureType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>matricula_inicial | traspasos | otros</summary>
    public string Family { get; set; } = string.Empty;
    /// <summary>global | company | ot | company_ot</summary>
    public string Scope { get; set; } = "global";
    /// <summary>ID de company u OT cuando scope != global.</summary>
    public Guid? ScopeRefId { get; set; }
    /// <summary>placa | vin | placa_vin</summary>
    public string VehicleQueryKey { get; set; } = "placa";
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public ICollection<ProcedureStep> Steps { get; set; } = [];
    public ICollection<ApiConnector> ApiConnectors { get; set; } = [];
    public ICollection<RuleSet> RuleSets { get; set; } = [];
    public ICollection<ActorDefinition> ActorDefinitions { get; set; } = [];
    public ICollection<ProcedureTypeSnapshot> Snapshots { get; set; } = [];
}
