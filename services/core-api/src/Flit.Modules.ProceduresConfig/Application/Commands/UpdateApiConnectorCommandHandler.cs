using System.Text.Json;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC3 HU-9782 — PUT api-connector param_bindings</summary>
public sealed class UpdateApiConnectorCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<ApiConnectorDto, ProcedureTypeError>> HandleAsync(
        UpdateApiConnectorCommand command, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(command.ParamBindingsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return Result<ApiConnectorDto, ProcedureTypeError>.Failure(
                    ProcedureTypeError.InvalidParamBindingsJson);
        }
        catch (JsonException)
        {
            return Result<ApiConnectorDto, ProcedureTypeError>.Failure(
                ProcedureTypeError.InvalidParamBindingsJson);
        }

        var connector = await repository.FindApiConnectorAsync(
            command.ProcedureTypeId, command.ConnectorId, command.TenantId, ct);
        if (connector is null)
            return Result<ApiConnectorDto, ProcedureTypeError>.Failure(ProcedureTypeError.ApiConnectorNotFound);

        var now = clock.UtcNow;
        connector.ParamBindings = command.ParamBindingsJson;
        connector.UpdatedAt = now;
        connector.UpdatedBy = command.RequestedByUserId;

        await repository.UpdateApiConnectorAsync(connector, ct);
        return Result<ApiConnectorDto, ProcedureTypeError>.Success(
            ProcedureTypeMapper.ToApiConnectorDto(connector));
    }
}
