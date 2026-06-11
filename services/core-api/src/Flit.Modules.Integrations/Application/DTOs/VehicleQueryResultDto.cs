namespace Flit.Modules.Integrations.Application.DTOs;

/// <summary>DTO del resultado de consulta RUNT por placa/VIN.</summary>
public sealed record VehicleQueryResultDto(
    bool Found,
    string? Plate,
    string? Vin,
    string? Brand,
    string? Model,
    int? Year,
    string? Color,
    string? FuelType,
    string? OwnerDocument,
    string? OwnerName,
    string? Status,
    IReadOnlyList<string> Restrictions,
    string Provider,
    int DurationMs);
