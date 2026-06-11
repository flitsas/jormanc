namespace Flit.Modules.Integrations.Application.DTOs;

/// <summary>DTO de configuración de conector por tenant.</summary>
public sealed record ConnectorConfigDto(
    Guid Id,
    Guid TenantId,
    string ConnectorType,
    string Provider,
    bool IsPrimary,
    int Priority,
    int TimeoutMs,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
