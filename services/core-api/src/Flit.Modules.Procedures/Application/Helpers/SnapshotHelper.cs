using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;

namespace Flit.Modules.Procedures.Application.Helpers;

internal static class SnapshotHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static ProcedureTypeSnapshot BuildSnapshot(ProcedureType procedureType, DateTimeOffset now)
    {
        var dto = ProcedureTypeMapper.ToDto(procedureType);
        return new ProcedureTypeSnapshot
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = procedureType.Id,
            Version = procedureType.Version,
            SnapshotJson = JsonSerializer.Serialize(dto, JsonOptions),
            CreatedAt = now
        };
    }

    public static IReadOnlyList<ProcedureStepSummaryDto> ExtractSteps(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return [];

        try
        {
            var dto = JsonSerializer.Deserialize<ProcedureTypeDto>(snapshotJson, JsonOptions);
            if (dto?.Steps is null)
                return [];

            return dto.Steps
                .OrderBy(s => s.OrderIndex)
                .Select(s => new ProcedureStepSummaryDto(
                    s.Id, s.OrderIndex, s.Name, s.StepType, s.IsRequired))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? ExtractVehicleQueryKey(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return null;

        try
        {
            var dto = JsonSerializer.Deserialize<ProcedureTypeDto>(snapshotJson, JsonOptions);
            return dto?.VehicleQueryKey;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static SnapshotConfigDto ExtractSnapshotConfig(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return new SnapshotConfigDto([]);

        try
        {
            var dto = JsonSerializer.Deserialize<ProcedureTypeDto>(snapshotJson, JsonOptions);
            if (dto?.Steps is null)
                return new SnapshotConfigDto([]);

            var steps = dto.Steps
                .OrderBy(s => s.OrderIndex)
                .Select(s => new ProcedureStepConfigDto(
                    s.Id,
                    s.OrderIndex,
                    s.Name,
                    s.StepType,
                    s.IsRequired,
                    s.Sections
                        .OrderBy(sec => sec.OrderIndex)
                        .Select(sec => new FormSectionConfigDto(
                            sec.Id,
                            sec.StepId,
                            sec.OrderIndex,
                            sec.Slug,
                            sec.Name,
                            sec.IsCollapsible,
                            sec.Fields
                                .OrderBy(f => f.OrderIndex)
                                .Select(f => new FormFieldConfigDto(
                                    f.Id,
                                    f.SectionId,
                                    f.OrderIndex,
                                    f.Slug,
                                    f.Name,
                                    f.FieldType,
                                    f.IsRequired,
                                    ParseFieldConfig(f.Config)))
                                .ToList()))
                        .ToList()))
                .ToList();

            return new SnapshotConfigDto(steps);
        }
        catch (JsonException)
        {
            return new SnapshotConfigDto([]);
        }
    }

    private static object? ParseFieldConfig(string config)
    {
        if (string.IsNullOrWhiteSpace(config))
            return new { };

        try
        {
            return JsonSerializer.Deserialize<object>(config, JsonOptions);
        }
        catch (JsonException)
        {
            return new { };
        }
    }
}
