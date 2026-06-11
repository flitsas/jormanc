namespace Flit.Modules.Integrations.Domain.Models;

/// <summary>
/// Resultado de una consulta de vehículo al RUNT.
/// Los campos reflejan el subconjunto de datos que FLIT 2.0 necesita para el stepper de trámites.
/// @pii:high — contiene datos de propietario.
/// </summary>
public sealed record VehicleQueryResult(
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
    string? RawJson);
