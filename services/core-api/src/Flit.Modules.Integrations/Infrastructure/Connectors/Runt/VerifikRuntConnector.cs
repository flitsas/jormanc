using System.Net.Http.Json;
using System.Text.Json;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flit.Modules.Integrations.Infrastructure.Connectors.Runt;

/// <summary>
/// Conector RUNT vía Verifik (proveedor primario).
/// Credenciales desde IOptions&lt;VerifikOptions&gt; (variables de entorno VERIFIK_API_KEY).
/// ADR-0011 C5: .env.verifik contiene credenciales reales — nunca en código.
/// En modo mock (ApiKey = "mock"), retorna datos ficticios sin llamada HTTP.
/// </summary>
public sealed class VerifikRuntConnector(
    IHttpClientFactory httpClientFactory,
    IOptions<VerifikOptions> options,
    ILogger<VerifikRuntConnector> logger) : IRuntConnector
{
    private readonly VerifikOptions _opts = options.Value;

    public string ProviderName => "verifik";

    public async Task<VehicleQueryResult> QueryVehicleByPlateAsync(
        string plate, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return MockVehicleResult(plate, "plate");

        var client = httpClientFactory.CreateClient("verifik");
        try
        {
            var response = await client.PostAsJsonAsync(
                "/v1/runt/vehicle-by-plate",
                new { plate, apiKey = _opts.ApiKey },
                ct);

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
            logger.LogWarning(ex, "Verifik RUNT error consultando placa {Plate}", plate);
            throw new ConnectorServerException(ProviderName, 503, "runt.vehicle.plate");
        }
    }

    public async Task<VehicleQueryResult> QueryVehicleByVinAsync(
        string vin, CancellationToken ct = default)
    {
        if (_opts.UseMock)
            return MockVehicleResult(vin, "vin");

        var client = httpClientFactory.CreateClient("verifik");
        var response = await client.PostAsJsonAsync(
            "/v1/runt/vehicle-by-vin",
            new { vin, apiKey = _opts.ApiKey },
            ct);

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

        var client = httpClientFactory.CreateClient("verifik");
        var response = await client.PostAsJsonAsync(
            "/v1/runt/person",
            new { documentNumber, apiKey = _opts.ApiKey },
            ct);

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

        var client = httpClientFactory.CreateClient("verifik");
        var response = await client.PostAsJsonAsync(
            "/v1/runt/restrictions",
            new { documentNumber, apiKey = _opts.ApiKey },
            ct);

        if ((int)response.StatusCode >= 500)
            throw new ConnectorServerException(ProviderName, (int)response.StatusCode,
                "runt.restrictions");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseRestrictionResponse(json, documentNumber);
    }

    // ─── Parsers stub (reemplazar con mapeo real de la API Verifik) ──────────

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
            Brand: "VERIFIK-MOCK", Model: "TEST", Year: 2023, Color: "AZUL",
            FuelType: "GASOLINA", OwnerDocument: "98765432", OwnerName: "VERIFIK MOCK OWNER",
            Status: "ACTIVO", Restrictions: [],
            RawJson: $"{{\"source\":\"verifik-mock\",\"key\":\"{key}\"}}");

    private static PersonQueryResult MockPersonResult(string doc) =>
        new(Found: true, DocumentNumber: doc, FullName: "VERIFIK MOCK PERSON",
            LicenseCategory: "B2", LicenseStatus: "VIGENTE",
            LicenseExpiry: new DateOnly(2029, 6, 30),
            Restrictions: [],
            RawJson: $"{{\"source\":\"verifik-mock\",\"document\":\"{doc}\"}}");
}

/// <summary>
/// Opciones para el conector Verifik (sección "Verifik" en configuración).
/// Credenciales desde variables de entorno VERIFIK_API_KEY (no en appsettings.json).
/// </summary>
public sealed class VerifikOptions
{
    public const string SectionName = "Verifik";
    public string BaseUrl { get; set; } = "https://api.verifik.co";
    /// <summary>API key de Verifik. Valor "mock" activa el mock sin llamadas HTTP.</summary>
    public string ApiKey { get; set; } = "mock";
    public bool UseMock => ApiKey == "mock" || string.IsNullOrWhiteSpace(ApiKey);
}
