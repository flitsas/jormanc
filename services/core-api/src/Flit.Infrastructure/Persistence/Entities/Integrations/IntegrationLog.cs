namespace Flit.Infrastructure.Persistence.Entities.Integrations;

/// <summary>
/// Log inmutable de llamadas a conectores externos (RUNT, SIMIT, RUES).
/// Contiene payloads completos — PII en runt_payload: no exponer sin autorización.
/// schema: integrations / tabla: integration_logs
/// </summary>
public sealed class IntegrationLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    /// <summary>runt | simit | rues | identity | quipux</summary>
    public string ConnectorType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    /// <summary>JSONB — request al sistema externo. @pii:high si contiene datos de persona.</summary>
    public string? RequestPayload { get; set; }
    /// <summary>JSONB — respuesta del sistema externo. @pii:high si contiene datos de persona.</summary>
    public string? ResponsePayload { get; set; }
    public int? HttpStatus { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
}
