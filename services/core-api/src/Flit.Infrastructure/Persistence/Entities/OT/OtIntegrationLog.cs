namespace Flit.Infrastructure.Persistence.Entities.OT;

/// <summary>
/// Log de integración Quipux (inmutable). Contiene payloads de webhook.
/// schema: ot / tabla: ot_integration_logs
/// </summary>
public sealed class OtIntegrationLog
{
    public Guid Id { get; set; }
    public Guid OtId { get; set; }
    public Guid TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ProcedureRef { get; set; }
    /// <summary>JSONB — payload recibido del webhook.</summary>
    public string? RequestPayload { get; set; }
    /// <summary>JSONB — respuesta enviada.</summary>
    public string? ResponsePayload { get; set; }
    public int? HttpStatus { get; set; }
    public int? DurationMs { get; set; }
    public DateTimeOffset LoggedAt { get; set; }

    public OtOrganism OtOrganism { get; set; } = null!;
}
