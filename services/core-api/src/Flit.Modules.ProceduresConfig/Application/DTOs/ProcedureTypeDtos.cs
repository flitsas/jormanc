namespace Flit.Modules.ProceduresConfig.Application.DTOs;

public sealed record ProcedureTypeDto(
    Guid Id,
    Guid TenantId,
    string Slug,
    string Name,
    string Family,
    string Scope,
    Guid? ScopeRefId,
    string VehicleQueryKey,
    int Version,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ProcedureStepDto> Steps,
    IReadOnlyList<ApiConnectorDto> ApiConnectors);

public sealed record ProcedureStepDto(
    Guid Id,
    Guid ProcedureTypeId,
    int OrderIndex,
    string Name,
    string StepType,
    bool IsRequired,
    IReadOnlyList<FormSectionDto> Sections);

public sealed record FormSectionDto(
    Guid Id,
    Guid StepId,
    int OrderIndex,
    string Slug,
    string Name,
    bool IsCollapsible,
    IReadOnlyList<FormFieldDto> Fields);

public sealed record FormFieldDto(
    Guid Id,
    Guid SectionId,
    int OrderIndex,
    string Slug,
    string Name,
    string FieldType,
    bool IsRequired,
    string Config);
