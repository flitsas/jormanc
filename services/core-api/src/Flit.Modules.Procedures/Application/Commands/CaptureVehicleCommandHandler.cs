using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Helpers;
using Flit.Modules.Procedures.Application.Mapping;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Commands;

public sealed class CaptureVehicleCommandHandler(
    IProcedureRepository procedureRepository,
    IProcedureTypeRepository procedureTypeRepository,
    IVehicleQueryService vehicleQueryService,
    IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<CaptureVehicleResponseDto, ProcedureError>> HandleAsync(
        CaptureVehicleCommand command, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<CaptureVehicleResponseDto, ProcedureError>.Failure(ProcedureError.NotFound);

        if (procedure.Status != "draft")
            return Result<CaptureVehicleResponseDto, ProcedureError>.Failure(ProcedureError.InvalidStatus);

        var snapshot = await procedureTypeRepository.FindSnapshotByIdAsync(
            procedure.ProcedureTypeSnapshotId, ct);
        var vehicleQueryKey = snapshot is not null
            ? SnapshotHelper.ExtractVehicleQueryKey(snapshot.SnapshotJson) ?? "placa"
            : "placa";

        VehicleCaptureResult capture;
        string queryKey;
        string queryValue;

        if (vehicleQueryKey is "vin" or "placa_vin" && !string.IsNullOrWhiteSpace(command.Vin))
        {
            queryKey = "vin";
            queryValue = command.Vin.Trim().ToUpperInvariant();
            capture = await vehicleQueryService.QueryByVinAsync(command.TenantId, queryValue, ct);
        }
        else if (!string.IsNullOrWhiteSpace(command.Plate))
        {
            queryKey = "placa";
            queryValue = command.Plate.Trim().ToUpperInvariant();
            capture = await vehicleQueryService.QueryByPlateAsync(command.TenantId, queryValue, ct);
        }
        else
        {
            return Result<CaptureVehicleResponseDto, ProcedureError>.Failure(ProcedureError.VehicleQueryFailed);
        }

        if (!capture.Found)
            return Result<CaptureVehicleResponseDto, ProcedureError>.Failure(ProcedureError.VehicleQueryFailed);

        var now = clock.UtcNow;
        var vehicleQuery = new VehicleQuery
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedure.Id,
            TenantId = command.TenantId,
            QueryKey = queryKey,
            QueryValue = queryValue,
            RuntPayload = capture.RuntPayloadJson,
            SimitPayload = capture.SimitPayloadJson,
            Warnings = JsonSerializer.Serialize(capture.Warnings, JsonOptions),
            QueriedAt = now
        };

        await procedureRepository.AddVehicleQueryAsync(vehicleQuery, ct);

        return Result<CaptureVehicleResponseDto, ProcedureError>.Success(
            new CaptureVehicleResponseDto(
                ProcedureId: procedure.Id,
                Status: procedure.Status,
                Vehicle: ProcedureMapper.ToVehicleDto(capture.Vehicle),
                Warnings: capture.Warnings));
    }

}
