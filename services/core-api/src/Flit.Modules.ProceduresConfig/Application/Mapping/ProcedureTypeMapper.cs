using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;

namespace Flit.Modules.ProceduresConfig.Application.Mapping;

public static class ProcedureTypeMapper
{
    public static ProcedureTypeDto ToDto(ProcedureType entity) =>
        new(
            Id: entity.Id,
            TenantId: entity.TenantId,
            Slug: entity.Slug,
            Name: entity.Name,
            Family: entity.Family,
            Scope: entity.Scope,
            ScopeRefId: entity.ScopeRefId,
            VehicleQueryKey: entity.VehicleQueryKey,
            Version: entity.Version,
            IsActive: entity.IsActive,
            CreatedAt: entity.CreatedAt,
            Steps: entity.Steps
                .OrderBy(s => s.OrderIndex)
                .Select(ToStepDto)
                .ToList(),
            ApiConnectors: entity.ApiConnectors
                .Where(c => c.IsActive)
                .OrderBy(c => c.StepOrder)
                .Select(ToApiConnectorDto)
                .ToList());

    public static ProcedureStepDto ToStepDto(ProcedureStep step) =>
        new(
            Id: step.Id,
            ProcedureTypeId: step.ProcedureTypeId,
            OrderIndex: step.OrderIndex,
            Name: step.Name,
            StepType: step.StepType,
            IsRequired: step.IsRequired,
            Sections: step.FormSections
                .OrderBy(sec => sec.OrderIndex)
                .Select(ToSectionDto)
                .ToList());

    public static FormSectionDto ToSectionDto(FormSection section) =>
        new(
            Id: section.Id,
            StepId: section.StepId,
            OrderIndex: section.OrderIndex,
            Slug: section.Slug,
            Name: section.Name,
            IsCollapsible: section.IsCollapsible,
            Fields: section.FormFields
                .Where(f => f.DeletedAt == null)
                .OrderBy(f => f.OrderIndex)
                .Select(ToFieldDto)
                .ToList());

    public static FormFieldDto ToFieldDto(FormField field) =>
        new(
            Id: field.Id,
            SectionId: field.SectionId,
            OrderIndex: field.OrderIndex,
            Slug: field.Slug,
            Name: field.Name,
            FieldType: field.FieldType,
            IsRequired: field.IsRequired,
            Config: field.Config);

    public static ApiConnectorDto ToApiConnectorDto(ApiConnector connector) =>
        new(
            connector.Id,
            connector.ProcedureTypeId,
            connector.TenantId,
            connector.Name,
            connector.Endpoint,
            connector.HttpVerb,
            connector.StepOrder,
            connector.ParamBindings,
            connector.ResponseMappings,
            connector.IsActive,
            connector.CreatedAt);
}
