namespace Flit.Modules.Procedures.Domain.Interfaces;

public sealed record VehicleCaptureResult(
    bool Found,
    string RuntPayloadJson,
    string? SimitPayloadJson,
    IReadOnlyList<string> Warnings,
    VehicleSummary Vehicle);

public sealed record VehicleSummary(
    string? Plate,
    string? Vin,
    string? Brand,
    string? Model,
    int? Year,
    string? Color,
    string? OwnerName);

public interface IVehicleQueryService
{
    Task<VehicleCaptureResult> QueryByPlateAsync(Guid tenantId, string plate, CancellationToken ct = default);

    Task<VehicleCaptureResult> QueryByVinAsync(Guid tenantId, string vin, CancellationToken ct = default);
}
