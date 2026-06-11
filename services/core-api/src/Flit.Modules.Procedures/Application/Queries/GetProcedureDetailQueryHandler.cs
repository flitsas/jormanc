using System.Text.Json;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Helpers;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Queries;

public sealed class GetProcedureDetailQueryHandler(
    IProcedureRepository procedureRepository,
    IProcedureTypeRepository procedureTypeRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<ProcedureDetailDto, ProcedureError>> HandleAsync(
        GetProcedureDetailQuery query, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            query.ProcedureId, query.TenantId, ct);
        if (procedure is null)
            return Result<ProcedureDetailDto, ProcedureError>.Failure(ProcedureError.NotFound);

        var snapshot = await procedureTypeRepository.FindSnapshotByIdAsync(
            procedure.ProcedureTypeSnapshotId, ct);
        if (snapshot is null)
            return Result<ProcedureDetailDto, ProcedureError>.Failure(ProcedureError.NotFound);

        var vehicleQueryKey = SnapshotHelper.ExtractVehicleQueryKey(snapshot.SnapshotJson) ?? "placa";
        var snapshotConfig = SnapshotHelper.ExtractSnapshotConfig(snapshot.SnapshotJson);
        var stepData = ParseStepData(procedure.StepData);

        return Result<ProcedureDetailDto, ProcedureError>.Success(
            new ProcedureDetailDto(
                Id: procedure.Id,
                CompositeId: procedure.CompositeId,
                Status: procedure.Status,
                ProcedureTypeSnapshotId: procedure.ProcedureTypeSnapshotId,
                VehicleQueryKey: vehicleQueryKey,
                CurrentStepOrder: procedure.CurrentStepOrder,
                StepData: stepData,
                SnapshotConfig: snapshotConfig));
    }

    private static object ParseStepData(string stepDataJson)
    {
        if (string.IsNullOrWhiteSpace(stepDataJson))
            return new { };

        try
        {
            return JsonSerializer.Deserialize<object>(stepDataJson, JsonOptions) ?? new { };
        }
        catch (JsonException)
        {
            return new { };
        }
    }
}
