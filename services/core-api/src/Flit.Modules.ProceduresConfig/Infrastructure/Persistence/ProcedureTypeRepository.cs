using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.ProceduresConfig.Infrastructure.Persistence;

public sealed class ProcedureTypeRepository(FlitDbContext db) : IProcedureTypeRepository
{
    public Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken ct = default) =>
        db.ProcedureTypes.AnyAsync(t => t.TenantId == tenantId && t.Slug == slug, ct);

    public Task<ProcedureType?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureTypes.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

    public Task<ProcedureType?> FindByIdWithDetailsAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureTypes
            .Include(t => t.Steps)
                .ThenInclude(s => s.FormSections)
                    .ThenInclude(sec => sec.FormFields)
            .Include(t => t.ApiConnectors)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

    public async Task CreateAsync(ProcedureType procedureType, CancellationToken ct = default)
    {
        db.ProcedureTypes.Add(procedureType);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddStepAsync(ProcedureStep procedureStep, CancellationToken ct = default)
    {
        db.ProcedureSteps.Add(procedureStep);
        await db.SaveChangesAsync(ct);
    }

    public Task<ProcedureStep?> FindStepAsync(
        Guid procedureTypeId, Guid stepId, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureSteps.FirstOrDefaultAsync(
            s => s.Id == stepId && s.ProcedureTypeId == procedureTypeId && s.TenantId == tenantId, ct);

    public async Task AddSectionAsync(FormSection section, CancellationToken ct = default)
    {
        db.FormSections.Add(section);
        await db.SaveChangesAsync(ct);
    }

    public Task<FormSection?> FindSectionAsync(
        Guid stepId, Guid sectionId, Guid tenantId, CancellationToken ct = default) =>
        db.FormSections.FirstOrDefaultAsync(
            s => s.Id == sectionId && s.StepId == stepId && s.TenantId == tenantId, ct);

    public async Task AddFieldAsync(FormField field, CancellationToken ct = default)
    {
        db.FormFields.Add(field);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> HasActiveProceduresAsync(Guid procedureTypeId, CancellationToken ct = default) =>
        db.Procedures.AnyAsync(
            p => p.ProcedureTypeId == procedureTypeId &&
                 (p.Status == "draft" || p.Status == "submitted"),
            ct);

    public async Task SoftDeleteAsync(
        ProcedureType procedureType, Guid deletedBy, DateTimeOffset deletedAt, CancellationToken ct = default)
    {
        procedureType.DeletedAt = deletedAt;
        procedureType.DeletedBy = deletedBy;
        procedureType.IsActive = false;
        procedureType.UpdatedAt = deletedAt;
        procedureType.UpdatedBy = deletedBy;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RuleSet>> GetRuleSetsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.RuleSets
            .Where(r => r.ProcedureTypeId == procedureTypeId && r.TenantId == tenantId && r.IsActive)
            .ToListAsync(ct);

    public async Task AddRuleSetAndIncrementVersionAsync(
        RuleSet ruleSet, ProcedureType procedureType, CancellationToken ct = default)
    {
        procedureType.Version += 1;
        procedureType.UpdatedAt = ruleSet.UpdatedAt;
        procedureType.UpdatedBy = ruleSet.UpdatedBy;
        db.RuleSets.Add(ruleSet);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddActorAsync(ActorDefinition actor, CancellationToken ct = default)
    {
        db.ActorDefinitions.Add(actor);
        await db.SaveChangesAsync(ct);
    }

    public Task<ActorDefinition?> FindActorAsync(
        Guid procedureTypeId, Guid actorId, Guid tenantId, CancellationToken ct = default) =>
        db.ActorDefinitions.FirstOrDefaultAsync(
            a => a.Id == actorId && a.ProcedureTypeId == procedureTypeId && a.TenantId == tenantId, ct);

    public async Task AddQueryRuleAsync(QueryRule queryRule, CancellationToken ct = default)
    {
        db.QueryRules.Add(queryRule);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateVehicleQueryKeyAsync(
        ProcedureType procedureType, string vehicleQueryKey, Guid updatedBy, DateTimeOffset updatedAt,
        CancellationToken ct = default)
    {
        procedureType.VehicleQueryKey = vehicleQueryKey;
        procedureType.Version += 1;
        procedureType.UpdatedAt = updatedAt;
        procedureType.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);
    }

    public Task<FormField?> FindFieldAsync(
        Guid sectionId, Guid fieldId, Guid tenantId, CancellationToken ct = default) =>
        db.FormFields.FirstOrDefaultAsync(
            f => f.Id == fieldId && f.SectionId == sectionId && f.TenantId == tenantId && f.DeletedAt == null,
            ct);

    public async Task UpdateStepOrderAsync(
        Guid procedureTypeId, Guid stepId, int newOrderIndex, Guid tenantId, CancellationToken ct = default)
    {
        var steps = await db.ProcedureSteps
            .Where(s => s.ProcedureTypeId == procedureTypeId && s.TenantId == tenantId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync(ct);

        var step = steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null) return;

        var oldIndex = step.OrderIndex;
        if (oldIndex == newOrderIndex) return;

        foreach (var s in steps)
        {
            if (s.Id == stepId) continue;
            if (newOrderIndex < oldIndex)
            {
                if (s.OrderIndex >= newOrderIndex && s.OrderIndex < oldIndex)
                    s.OrderIndex++;
            }
            else if (s.OrderIndex > oldIndex && s.OrderIndex <= newOrderIndex)
            {
                s.OrderIndex--;
            }
        }

        step.OrderIndex = newOrderIndex;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateFieldAsync(FormField field, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task AddApiConnectorAsync(ApiConnector connector, CancellationToken ct = default)
    {
        db.ApiConnectors.Add(connector);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ApiConnector>> GetApiConnectorsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.ApiConnectors
            .Where(c => c.ProcedureTypeId == procedureTypeId && c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.StepOrder)
            .ToListAsync(ct);

    public Task<ApiConnector?> FindApiConnectorAsync(
        Guid procedureTypeId, Guid connectorId, Guid tenantId, CancellationToken ct = default) =>
        db.ApiConnectors.FirstOrDefaultAsync(
            c => c.Id == connectorId && c.ProcedureTypeId == procedureTypeId && c.TenantId == tenantId, ct);

    public async Task UpdateApiConnectorAsync(ApiConnector connector, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public Task<ProcedureTypeSnapshot?> FindSnapshotAsync(
        Guid procedureTypeId, int version, CancellationToken ct = default) =>
        db.ProcedureTypeSnapshots.FirstOrDefaultAsync(
            s => s.ProcedureTypeId == procedureTypeId && s.Version == version, ct);

    public Task<ProcedureTypeSnapshot?> FindSnapshotByIdAsync(Guid snapshotId, CancellationToken ct = default) =>
        db.ProcedureTypeSnapshots.FirstOrDefaultAsync(s => s.Id == snapshotId, ct);

    public async Task AddSnapshotAsync(ProcedureTypeSnapshot snapshot, CancellationToken ct = default)
    {
        db.ProcedureTypeSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ActorDefinition>> GetActorDefinitionsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.ActorDefinitions
            .AsNoTracking()
            .Where(a => a.ProcedureTypeId == procedureTypeId && a.TenantId == tenantId)
            .OrderBy(a => a.OrderIndex)
            .ToListAsync(ct);
}
