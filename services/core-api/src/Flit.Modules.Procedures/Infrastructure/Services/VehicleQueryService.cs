using System.Text.Json;
using Flit.Modules.Integrations.Application.UseCases;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

public sealed class VehicleQueryService(QueryVehicleByPlateUseCase queryByPlateUseCase) : IVehicleQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<VehicleCaptureResult> QueryByPlateAsync(
        Guid tenantId, string plate, CancellationToken ct = default)
    {
        var result = await queryByPlateUseCase.ExecuteAsync(tenantId, plate, ct);
        if (!result.IsSuccess)
            return FailedResult(plate, null);

        var dto = result.Value;
        var warnings = BuildWarnings(dto.Restrictions);
        var runtJson = JsonSerializer.Serialize(dto, JsonOptions);

        return new VehicleCaptureResult(
            Found: dto.Found,
            RuntPayloadJson: runtJson,
            SimitPayloadJson: null,
            Warnings: warnings,
            Vehicle: new VehicleSummary(
                Plate: dto.Plate,
                Vin: dto.Vin,
                Brand: dto.Brand,
                Model: dto.Model,
                Year: dto.Year,
                Color: dto.Color,
                OwnerName: dto.OwnerName));
    }

    public async Task<VehicleCaptureResult> QueryByVinAsync(
        Guid tenantId, string vin, CancellationToken ct = default)
    {
        // Reutiliza el use case de placa con VIN hasta exponer QueryVehicleByVinUseCase dedicado.
        var result = await queryByPlateUseCase.ExecuteAsync(tenantId, vin, ct);
        if (!result.IsSuccess)
            return FailedResult(null, vin);

        var dto = result.Value;
        var warnings = BuildWarnings(dto.Restrictions);
        var runtJson = JsonSerializer.Serialize(dto, JsonOptions);

        return new VehicleCaptureResult(
            Found: dto.Found,
            RuntPayloadJson: runtJson,
            SimitPayloadJson: null,
            Warnings: warnings,
            Vehicle: new VehicleSummary(
                Plate: dto.Plate,
                Vin: dto.Vin ?? vin,
                Brand: dto.Brand,
                Model: dto.Model,
                Year: dto.Year,
                Color: dto.Color,
                OwnerName: dto.OwnerName));
    }

    private static VehicleCaptureResult FailedResult(string? plate, string? vin) =>
        new(
            Found: false,
            RuntPayloadJson: "{}",
            SimitPayloadJson: null,
            Warnings: [],
            Vehicle: new VehicleSummary(plate, vin, null, null, null, null, null));

    private static List<string> BuildWarnings(IReadOnlyList<string> restrictions)
    {
        var warnings = new List<string>();
        foreach (var restriction in restrictions)
        {
            if (!string.IsNullOrWhiteSpace(restriction))
                warnings.Add($"Restricción: {restriction.Trim().ToUpperInvariant()}");
        }

        return warnings;
    }
}
