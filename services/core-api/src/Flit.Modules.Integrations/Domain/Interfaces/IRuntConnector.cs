using Flit.Modules.Integrations.Domain.Models;

namespace Flit.Modules.Integrations.Domain.Interfaces;

/// <summary>
/// Contrato del conector RUNT (ADR-0011 — Strategy + hot-failover).
/// Implementaciones: VerifikRuntConnector (primario), IntempoRuntConnector (secundario),
/// MockRuntConnector (DEV/TEST).
/// El ConnectorRouter implementa esta interfaz y orquesta el failover automático.
/// </summary>
public interface IRuntConnector
{
    /// <summary>Consulta vehículo por placa. AC1 HU-9775.</summary>
    Task<VehicleQueryResult> QueryVehicleByPlateAsync(string plate, CancellationToken ct = default);

    /// <summary>Consulta vehículo por VIN. AC1 HU-9775.</summary>
    Task<VehicleQueryResult> QueryVehicleByVinAsync(string vin, CancellationToken ct = default);

    /// <summary>Consulta persona (conductor) por número de documento. AC1 HU-9775.</summary>
    Task<PersonQueryResult> QueryPersonAsync(string documentNumber, CancellationToken ct = default);

    /// <summary>Consulta restricciones activas sobre un documento.</summary>
    Task<RestrictionQueryResult> QueryRestrictionsAsync(string documentNumber, CancellationToken ct = default);

    /// <summary>Nombre del proveedor — "verifik" | "intempo" | "mock". Usado en IntegrationLog.</summary>
    string ProviderName { get; }
}
