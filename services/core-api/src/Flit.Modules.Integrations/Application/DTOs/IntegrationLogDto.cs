namespace Flit.Modules.Integrations.Application.DTOs;

/// <summary>
/// DTO de log de integración para lectura (sin credenciales ni tokens).
/// @pii:high — RequestPayload/ResponsePayload pueden contener datos de vehículo/persona.
/// Solo exponer a SuperAdmin.
/// </summary>
public sealed record IntegrationLogDto(
    Guid Id,
    Guid TenantId,
    string ConnectorType,
    string Operation,
    string Provider,
    string? RequestPayload,
    string? ResponsePayload,
    int? HttpStatus,
    int DurationMs,
    string? ErrorMessage,
    DateTimeOffset LoggedAt);
