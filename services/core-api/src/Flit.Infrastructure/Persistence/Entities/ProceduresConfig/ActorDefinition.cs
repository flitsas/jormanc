namespace Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

/// <summary>
/// Definición de actor en un tipo de trámite (vendedor, comprador, vehículo, etc.).
/// schema: procedures_config / tabla: actor_definitions
/// </summary>
public sealed class ActorDefinition
{
    public Guid Id { get; set; }
    public Guid ProcedureTypeId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>vendedor | comprador | vehiculo | representante_legal</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>natural | juridica | ambas | vehiculo</summary>
    public string AllowedNature { get; set; } = "ambas";
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
    public int OrderIndex { get; set; }
    /// <summary>Para persona jurídica: FK a actor representante legal.</summary>
    public Guid? LegalRepActorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public ProcedureType ProcedureType { get; set; } = null!;
    public ActorDefinition? LegalRepActor { get; set; }
    public ICollection<QueryRule> QueryRules { get; set; } = [];
}
