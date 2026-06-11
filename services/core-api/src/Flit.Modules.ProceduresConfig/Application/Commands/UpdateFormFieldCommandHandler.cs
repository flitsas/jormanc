using System.Text.Json;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC2 HU-9782 — PUT field con config dropdown</summary>
public sealed class UpdateFormFieldCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidFieldTypes =
        ["text", "dropdown", "checkbox", "numeric", "attachment", "list"];

    public async Task<Result<FormFieldDto, ProcedureTypeError>> HandleAsync(
        UpdateFormFieldCommand command, CancellationToken ct = default)
    {
        var section = await repository.FindSectionAsync(command.StepId, command.SectionId, command.TenantId, ct);
        if (section is null)
            return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.SectionNotFound);

        var field = await repository.FindFieldAsync(command.SectionId, command.FieldId, command.TenantId, ct);
        if (field is null)
            return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.FieldNotFound);

        if (command.FieldType is not null)
        {
            var ft = command.FieldType.Trim().ToLowerInvariant();
            if (!ValidFieldTypes.Contains(ft))
                return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidFieldType);
            field.FieldType = ft;
        }

        if (command.ConfigJson is not null)
        {
            try
            {
                JsonDocument.Parse(command.ConfigJson);
            }
            catch (JsonException)
            {
                return Result<FormFieldDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidConfigJson);
            }

            field.Config = command.ConfigJson;
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
            field.Name = command.Name.Trim();

        if (command.IsRequired.HasValue)
            field.IsRequired = command.IsRequired.Value;

        var now = clock.UtcNow;
        field.UpdatedAt = now;
        field.UpdatedBy = command.RequestedByUserId;

        await repository.UpdateFieldAsync(field, ct);
        return Result<FormFieldDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToFieldDto(field));
    }
}
