using System.Net.Http.Json;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flit.Modules.Integrations.Infrastructure.Connectors.Runt;

/// <summary>
/// Conector RUNT vía Intempo (proveedor secundario / failover).
/// Se activa automáticamente cuando Verifik falla por timeout o HTTP 5xx (AC2 HU-9775).
/// En modo mock (ApiKey = "mock"), retorna datos ficticios sin llamada HTTP.
/// Credenciales desde IOptions&lt;IntempoOptions&gt; (variable INTEMPO_API_KEY).
/// </summary>
public sealed class IntempoRuntConnector(
    IHttpClientFactory httpClientFactory,
    IOptions<IntempoOptions> options,
    ILogger<IntempoRuntConnector> logger) : IRuntConnector
{
    private readonly IntempoOptions _opts = options.Value;

    public string ProviderName => "intempo";

    public async Task<VehicleQueryResult> QueryVehicleByPlateAsync(
        string plate, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return MockVehicleResult(plate, "plate");

        var client = httpClientFactory.CreateClient("intempo");
        try
        {
            var response = await client.GetAsync(
                $"/runt/vehicles/{Uri.EscapeDataString(plate)}", ct);

            if ((int)response.StatusCode >= 500)
                throw new ConnectorServerException(ProviderName, (int)response.StatusCode,
                    "runt.vehicle.plate");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return ParseVehicleResponse(json, plate);
        }
        catch (ConnectorServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Intempo RUNT error consultando placa {Plate}", plate);
            throw new ConnectorServerException(ProviderName, 503, "runt.vehicle.plate");
        }
    }

    public async Task<VehicleQueryResult> QueryVehicleByVinAsync(
        string vin, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return MockVehicleResult(vin, "vin");

        var client = httpClientFactory.CreateClient("intempo");
        var response = await client.GetAsync($"/runt/vehicles/vin/{Uri.EscapeDataString(vin)}", ct);

        if ((int)response.StatusCode >= 500)
            throw new ConnectorServerException(ProviderName, (int)response.StatusCode,
                "runt.vehicle.vin");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseVehicleResponse(json, vin);
    }

    public async Task<PersonQueryResult> QueryPersonAsync(
        string documentNumber, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return MockPersonResult(documentNumber);

        var client = httpClientFactory.CreateClient("intempo");
        var response = await client.GetAsync(
            $"/runt/persons/{Uri.EscapeDataString(documentNumber)}", ct);

        if ((int)response.StatusCode >= 500)
            throw new ConnectorServerException(ProviderName, (int)response.StatusCode,
                "runt.person");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return ParsePersonResponse(json, documentNumber);
    }

    public async Task<RestrictionQueryResult> QueryRestrictionsAsync(
        string documentNumber, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return new RestrictionQueryResult(false, [], null);

        var client = httpClientFactory.CreateClient("intempo");
        var response = await client.GetAsync(
            $"/runt/restrictions/{Uri.EscapeDataString(documentNumber)}", ct);

        if ((int)response.StatusCode >= 500)
            throw new ConnectorServerException(ProviderName, (int)response.StatusCode,
                "runt.restrictions");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseRestrictionResponse(json, documentNumber);
    }

    // ─── Parsers stub ────────────────────────────────────────────────────────

    private static VehicleQueryResult ParseVehicleResponse(string json, string key) =>
        new(Found: true, Plate: key, Vin: null, Brand: null, Model: null, Year: null,
            Color: null, FuelType: null, OwnerDocument: null, OwnerName: null,
            Status: "ACTIVO", Restrictions: [], RawJson: json);

    private static PersonQueryResult ParsePersonResponse(string json, string documentNumber) =>
        new(Found: true, DocumentNumber: documentNumber, FullName: null,
            LicenseCategory: null, LicenseStatus: null, LicenseExpiry: null,
            Restrictions: [], RawJson: json);

    private static RestrictionQueryResult ParseRestrictionResponse(string json, string _) =>
        new(Found: false, Restrictions: [], RawJson: json);

    private static VehicleQueryResult MockVehicleResult(string key, string keyType) =>
        new(Found: true, Plate: keyType == "plate" ? key : null,
            Vin: keyType == "vin" ? key : null,
            Brand: "INTEMPO-MOCK", Model: "FALLBACK", Year: 2022, Color: "ROJO",
            FuelType: "GASOLINA", OwnerDocument: "11111111", OwnerName: "INTEMPO MOCK OWNER",
            Status: "ACTIVO", Restrictions: [],
            RawJson: $"{{\"source\":\"intempo-mock\",\"key\":\"{key}\"}}");

    private static PersonQueryResult MockPersonResult(string doc) =>
        new(Found: true, DocumentNumber: doc, FullName: "INTEMPO MOCK PERSON",
            LicenseCategory: "C1", LicenseStatus: "VIGENTE",
            LicenseExpiry: new DateOnly(2027, 3, 31),
            Restrictions: [],
            RawJson: $"{{\"source\":\"intempo-mock\",\"document\":\"{doc}\"}}");
}

/// <summary>Opciones para el conector Intempo.</summary>
public sealed class IntempoOptions
{
    public const string SectionName = "Intempo";
    public string BaseUrl { get; set; } = "https://api.intempo.co";
    /// <summary>API key de Intempo. Valor "mock" activa mock sin llamadas HTTP.</summary>
    public string ApiKey { get; set; } = "mock";
    public bool UseMock => ApiKey == "mock" || string.IsNullOrWhiteSpace(ApiKey);
}
