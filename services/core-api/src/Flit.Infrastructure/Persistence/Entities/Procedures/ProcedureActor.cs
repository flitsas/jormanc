namespace Flit.Infrastructure.Persistence.Entities.Procedures;

/// <summary>
/// Actor instanciado en un trámite (vendedor, comprador, representante legal, vehículo).
/// Los copropietarios usan cuota_pct; la suma de todos los compradores debe ser 100%.
/// schema: procedures / tabla: procedure_actors
/// </summary>
public sealed class ProcedureActor
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorDefinitionId { get; set; }
    /// <summary>natural | juridica | representante_legal | vehiculo</summary>
    public string Nature { get; set; } = string.Empty;
    /// <summary>Para representante_legal: FK al actor jurídico padre.</summary>
    public Guid? ParentActorId { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Nit { get; set; }
    public string? FullName { get; set; }
    /// <summary>Porcentaje de copropiedad; suma de compradores debe ser exactamente 100.</summary>
    public decimal? CuotaPct { get; set; }
    /// <summary>JSONB — resultados de consultas RUNT, SIMIT, RUES.</summary>
    public string QueryResults { get; set; } = "{}";
    public bool IdentityValidated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public int RowVersion { get; set; }

    public Procedure Procedure { get; set; } = null!;
    public ProcedureActor? ParentActor { get; set; }
}
