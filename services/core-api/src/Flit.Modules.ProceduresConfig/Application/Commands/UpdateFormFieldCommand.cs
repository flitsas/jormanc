namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record UpdateFormFieldCommand(
    Guid ProcedureTypeId,
    Guid StepId,
    Guid SectionId,
    Guid FieldId,
    Guid TenantId,
    Guid RequestedByUserId,
    string? Name,
    string? FieldType,
    bool? IsRequired,
    string? ConfigJson);
