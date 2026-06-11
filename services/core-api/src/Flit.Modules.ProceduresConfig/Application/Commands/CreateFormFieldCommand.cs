namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateFormFieldCommand(
    Guid ProcedureTypeId,
    Guid StepId,
    Guid SectionId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Slug,
    string Name,
    string FieldType,
    bool IsRequired,
    string ConfigJson,
    int OrderIndex);
