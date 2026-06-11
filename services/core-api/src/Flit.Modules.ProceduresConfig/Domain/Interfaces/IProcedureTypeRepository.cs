using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

namespace Flit.Modules.ProceduresConfig.Domain.Interfaces;

public sealed record ProcedureTypeListFilter(
    Guid TenantId,
    string? Family = null,
    string? Scope = null,
    int Page = 1,
    int PageSize = 20);

public interface IProcedureTypeRepository
{
    Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default);

    Task<ProcedureType?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<ProcedureType?> FindByIdWithDetailsAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task CreateAsync(ProcedureType procedureType, CancellationToken ct = default);

    Task AddStepAsync(ProcedureStep procedureStep, CancellationToken ct = default);

    Task<ProcedureStep?> FindStepAsync(Guid procedureTypeId, Guid stepId, Guid tenantId, CancellationToken ct = default);

    Task AddSectionAsync(FormSection section, CancellationToken ct = default);

    Task<FormSection?> FindSectionAsync(Guid stepId, Guid sectionId, Guid tenantId, CancellationToken ct = default);

    Task AddFieldAsync(FormField field, CancellationToken ct = default);

    Task<bool> HasActiveProceduresAsync(Guid procedureTypeId, CancellationToken ct = default);

    Task SoftDeleteAsync(ProcedureType procedureType, Guid deletedBy, DateTimeOffset deletedAt, CancellationToken ct = default);

    Task<IReadOnlyList<RuleSet>> GetRuleSetsAsync(Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task AddRuleSetAndIncrementVersionAsync(
        RuleSet ruleSet, ProcedureType procedureType, CancellationToken ct = default);

    Task AddActorAsync(ActorDefinition actor, CancellationToken ct = default);

    Task<ActorDefinition?> FindActorAsync(
        Guid procedureTypeId, Guid actorId, Guid tenantId, CancellationToken ct = default);

    Task AddQueryRuleAsync(QueryRule queryRule, CancellationToken ct = default);

    Task UpdateVehicleQueryKeyAsync(
        ProcedureType procedureType, string vehicleQueryKey, Guid updatedBy, DateTimeOffset updatedAt,
        CancellationToken ct = default);

    Task<FormField?> FindFieldAsync(
        Guid sectionId, Guid fieldId, Guid tenantId, CancellationToken ct = default);

    Task UpdateStepOrderAsync(
        Guid procedureTypeId, Guid stepId, int newOrderIndex, Guid tenantId, CancellationToken ct = default);

    Task UpdateFieldAsync(FormField field, CancellationToken ct = default);

    Task<IReadOnlyList<ApiConnector>> GetApiConnectorsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<ApiConnector?> FindApiConnectorAsync(
        Guid procedureTypeId, Guid connectorId, Guid tenantId, CancellationToken ct = default);

    Task UpdateApiConnectorAsync(ApiConnector connector, CancellationToken ct = default);

    Task<ProcedureTypeSnapshot?> FindSnapshotAsync(
        Guid procedureTypeId, int version, CancellationToken ct = default);

    Task<ProcedureTypeSnapshot?> FindSnapshotByIdAsync(Guid snapshotId, CancellationToken ct = default);

    Task AddSnapshotAsync(ProcedureTypeSnapshot snapshot, CancellationToken ct = default);

    Task<IReadOnlyList<ActorDefinition>> GetActorDefinitionsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);
}
