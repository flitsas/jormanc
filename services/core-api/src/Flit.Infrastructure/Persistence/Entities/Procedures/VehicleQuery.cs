namespace Flit.Infrastructure.Persistence.Entities.Procedures;

/// <summary>
/// Consulta de vehículo ejecutada contra RUNT/SIMIT. Payloads PII — no exponer sin autorización.
/// schema: procedures / tabla: vehicle_queries
/// </summary>
public sealed class VehicleQuery
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>placa | vin</summary>
    public string QueryKey { get; set; } = "placa";
    public string QueryValue { get; set; } = string.Empty;
    /// <summary>JSONB — respuesta completa de RUNT. @pii:high</summary>
    public string? RuntPayload { get; set; }
    /// <summary>JSONB — respuesta completa de SIMIT. @pii:medium</summary>
    public string? SimitPayload { get; set; }
    /// <summary>JSONB — respuesta completa de RUES. @pii:medium</summary>
    public string? RuesPayload { get; set; }
    /// <summary>JSONB — hallazgos no bloqueantes (multas, restricciones, embargos).</summary>
    public string Warnings { get; set; } = "[]";
    public DateTimeOffset QueriedAt { get; set; }

    public Procedure Procedure { get; set; } = null!;
}
