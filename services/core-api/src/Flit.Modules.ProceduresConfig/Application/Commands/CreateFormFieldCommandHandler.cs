using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC2 HU-9779 — campo dropdown con config JSONB</summary>
public sealed class CreateFormFieldCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidFieldTypes =
        ["text", "dropdown", "checkbox", "numeric", "attachment", "list"];

    public async Task<Result<FormFieldDto, ProcedureTypeError>> HandleAsync(
        CreateFormFieldCommand command, CancellationToken ct = default)
    {
        if (!ValidFieldTypes.Contains(command.FieldType))
            return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidFieldType);

        try
        {
            JsonDocument.Parse(command.ConfigJson);
        }
        catch (JsonException)
        {
            return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidConfigJson);
        }

        var section = await repository.FindSectionAsync(command.StepId, command.SectionId, command.TenantId, ct);
        if (section is null)
            return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.SectionNotFound);

        var now = clock.UtcNow;
        var field = new FormField
        {
            Id = Guid.NewGuid(),
            SectionId = command.SectionId,
            TenantId = command.TenantId,
            OrderIndex = command.OrderIndex,
            Slug = command.Slug.Trim().ToLowerInvariant(),
            Name = command.Name.Trim(),
            FieldType = command.FieldType.Trim().ToLowerInvariant(),
            IsRequired = command.IsRequired,
            Config = command.ConfigJson,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddFieldAsync(field, ct);
        return Result<FormFieldDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToFieldDto(field));
    }
}
