using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>POST /procedure-types/{id}/api-connectors — soporte E2E HU-9782 AC3</summary>
public sealed class CreateApiConnectorCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidHttpVerbs = ["GET", "POST", "PUT", "PATCH"];

    public async Task<Result<ApiConnectorDto, ProcedureTypeError>> HandleAsync(
        CreateApiConnectorCommand command, CancellationToken ct = default)
    {
        var procedureType = await repository.FindByIdAsync(command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<ApiConnectorDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        var verb = command.HttpVerb.Trim().ToUpperInvariant();
        if (!ValidHttpVerbs.Contains(verb))
            return Result<ApiConnectorDto, ProcedureTypeError>.Failure(
                new ProcedureTypeError("API_CONNECTOR_INVALID_HTTP_VERB", "http_verb inválido."));

        try
        {
            JsonDocument.Parse(command.ParamBindingsJson);
        }
        catch (JsonException)
        {
            return Result<ApiConnectorDto, ProcedureTypeError>.Failure(
                ProcedureTypeError.InvalidParamBindingsJson);
        }

        var now = clock.UtcNow;
        var connector = new ApiConnector
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = command.ProcedureTypeId,
            TenantId = command.TenantId,
            Name = command.Name.Trim(),
            Endpoint = command.Endpoint.Trim(),
            HttpVerb = verb,
            StepOrder = command.StepOrder < 1 ? 1 : command.StepOrder,
            ParamBindings = command.ParamBindingsJson,
            ResponseMappings = "{}",
            IsActive = true,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddApiConnectorAsync(connector, ct);
        return Result<ApiConnectorDto, ProcedureTypeError>.Success(
            ProcedureTypeMapper.ToApiConnectorDto(connector));
    }
}
