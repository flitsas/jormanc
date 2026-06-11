using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Domain.Models;

namespace Flit.Modules.Integrations.Infrastructure.Connectors.Runt;

/// <summary>
/// Conector RUNT de prueba — retorna datos ficticios.
/// Activado en DEV y entornos de test (C4 ADR-0011: mocks intercambiables).
/// NO hace llamadas HTTP reales.
/// </summary>
public sealed class MockRuntConnector : IRuntConnector
{
    public string ProviderName => "mock";

    public Task<VehicleQueryResult> QueryVehicleByPlateAsync(
        string plate, CancellationToken ct = default) =>
        Task.FromResult(new VehicleQueryResult(
            Found: true,
            Plate: plate.ToUpperInvariant(),
            Vin: "MOCK1234567890123",
            Brand: "TOYOTA",
            Model: "HILUX",
            Year: 2022,
            Color: "BLANCO",
            FuelType: "DIESEL",
            OwnerDocument: "79999999",
            OwnerName: "MOCK OWNER TEST",
            Status: "ACTIVO",
            Restrictions: Array.Empty<string>(),
            RawJson: $"{{\"source\":\"mock\",\"plate\":\"{plate}\"}}"));

    public Task<VehicleQueryResult> QueryVehicleByVinAsync(
        string vin, CancellationToken ct = default) =>
        Task.FromResult(new VehicleQueryResult(
            Found: true,
            Plate: "ABC123",
            Vin: vin,
            Brand: "RENAULT",
            Model: "SANDERO",
            Year: 2021,
            Color: "GRIS",
            FuelType: "GASOLINA",
            OwnerDocument: "12345678",
            OwnerName: "MOCK VIN OWNER",
            Status: "ACTIVO",
            Restrictions: Array.Empty<string>(),
            RawJson: $"{{\"source\":\"mock\",\"vin\":\"{vin}\"}}"));

    public Task<PersonQueryResult> QueryPersonAsync(
        string documentNumber, CancellationToken ct = default) =>
        Task.FromResult(new PersonQueryResult(
            Found: true,
            DocumentNumber: documentNumber,
            FullName: "MOCK PERSONA TEST",
            LicenseCategory: "B1",
            LicenseStatus: "VIGENTE",
            LicenseExpiry: new DateOnly(2028, 12, 31),
            Restrictions: Array.Empty<string>(),
            RawJson: $"{{\"source\":\"mock\",\"document\":\"{documentNumber}\"}}"));

    public Task<RestrictionQueryResult> QueryRestrictionsAsync(
        string documentNumber, CancellationToken ct = default) =>
        Task.FromResult(new RestrictionQueryResult(
            Found: false,
            Restrictions: Array.Empty<RestrictionItem>(),
            RawJson: $"{{\"source\":\"mock\",\"document\":\"{documentNumber}\",\"restrictions\":[]}}"));
}
