using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Documents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Documents.Infrastructure.Persistence;

public sealed class DocumentRepository(FlitDbContext db) : IDocumentRepository
{
    public async Task CreateDocumentTypeAsync(DocumentType documentType, CancellationToken ct = default)
    {
        db.DocumentTypes.Add(documentType);
        await db.SaveChangesAsync(ct);
    }

    public Task<DocumentType?> FindDocumentTypeByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        db.DocumentTypes.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

    public Task<bool> ProcedureTypeExistsAsync(Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureTypes.AnyAsync(p => p.Id == procedureTypeId && p.TenantId == tenantId, ct);

    public Task<bool> AssociationExistsAsync(
        Guid procedureTypeId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureTypeDocuments.AnyAsync(
            d => d.ProcedureTypeId == procedureTypeId &&
                 d.DocumentTypeId == documentTypeId &&
                 d.TenantId == tenantId,
            ct);

    public async Task CreateAssociationAsync(ProcedureTypeDocument association, CancellationToken ct = default)
    {
        db.ProcedureTypeDocuments.Add(association);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProcedureTypeDocument>> GetAssociationsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.ProcedureTypeDocuments
            .Include(d => d.DocumentType)
            .Where(d => d.ProcedureTypeId == procedureTypeId && d.TenantId == tenantId)
            .OrderBy(d => d.OrderIndex)
            .ToListAsync(ct);

    public Task<bool> ActorDefinitionBelongsToProcedureTypeAsync(
        Guid actorDefinitionId, Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.ActorDefinitions.AnyAsync(
            a => a.Id == actorDefinitionId &&
                 a.ProcedureTypeId == procedureTypeId &&
                 a.TenantId == tenantId,
            ct);

    public async Task<int> GetMaxTemplateVersionAsync(
        Guid documentTypeId, Guid tenantId, CancellationToken ct = default)
    {
        var max = await db.DocumentTemplates
            .Where(t => t.DocumentTypeId == documentTypeId && t.TenantId == tenantId)
            .Select(t => (int?)t.Version)
            .MaxAsync(ct);

        return max ?? 0;
    }

    public Task<DocumentTemplate?> FindActiveTemplateAsync(
        Guid documentTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.DocumentTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(
                t => t.DocumentTypeId == documentTypeId &&
                     t.TenantId == tenantId &&
                     t.Status == "active",
                ct);

    public async Task CreateTemplateAsync(
        DocumentTemplate documentTemplate, IReadOnlyList<TemplateField> fields, CancellationToken ct = default)
    {
        db.DocumentTemplates.Add(documentTemplate);
        if (fields.Count > 0)
            db.Set<TemplateField>().AddRange(fields);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeprecateActiveTemplatesAsync(
        Guid documentTypeId, Guid tenantId, CancellationToken ct = default)
    {
        var actives = await db.DocumentTemplates
            .Where(t => t.DocumentTypeId == documentTypeId &&
                        t.TenantId == tenantId &&
                        t.Status == "active")
            .ToListAsync(ct);

        if (actives.Count == 0) return;

        var now = DateTimeOffset.UtcNow;
        foreach (var template in actives)
        {
            template.Status = "deprecated";
            template.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentTemplate>> GetTemplatesAsync(
        Guid documentTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.DocumentTemplates
            .Include(t => t.Fields)
            .Where(t => t.DocumentTypeId == documentTypeId && t.TenantId == tenantId)
            .OrderByDescending(t => t.Version)
            .ToListAsync(ct);

    public Task<DocumentTemplate?> FindTemplateByIdAsync(
        Guid templateId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.DocumentTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(
                t => t.Id == templateId &&
                     t.DocumentTypeId == documentTypeId &&
                     t.TenantId == tenantId,
                ct);

    public Task<Procedure?> FindProcedureAsync(Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        db.Procedures.FirstOrDefaultAsync(
            p => p.Id == procedureId && p.TenantId == tenantId && p.DeletedAt == null,
            ct);

    public async Task<IReadOnlyList<ProcedureActor>> GetProcedureActorsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        await db.ProcedureActors
            .Where(a => a.ProcedureId == procedureId && a.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ActorDefinition>> GetActorDefinitionsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        await db.ActorDefinitions
            .Where(a => a.ProcedureTypeId == procedureTypeId && a.TenantId == tenantId)
            .ToListAsync(ct);

    public Task<VehicleQuery?> GetLatestVehicleQueryAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        db.VehicleQueries
            .Where(v => v.ProcedureId == procedureId && v.TenantId == tenantId)
            .OrderByDescending(v => v.QueriedAt)
            .FirstOrDefaultAsync(ct);

    public Task<OtOrganism?> FindOtAsync(Guid otId, Guid tenantId, CancellationToken ct = default) =>
        db.OtOrganisms.FirstOrDefaultAsync(
            o => o.Id == otId && o.TenantId == tenantId && o.DeletedAt == null,
            ct);

    public Task<ProcedureDocument?> FindProcedureDocumentAsync(
        Guid procedureId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureDocuments.FirstOrDefaultAsync(
            d => d.ProcedureId == procedureId &&
                 d.DocumentTypeId == documentTypeId &&
                 d.TenantId == tenantId,
            ct);

    public async Task CreateProcedureDocumentAsync(ProcedureDocument document, CancellationToken ct = default)
    {
        db.ProcedureDocuments.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateProcedureDocumentAsync(ProcedureDocument document, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProcedureDocument>> GetProcedureDocumentsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        await db.ProcedureDocuments
            .Include(d => d.DocumentType)
            .Where(d => d.ProcedureId == procedureId && d.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<int> GetMaxConsolidatedVersionAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default)
    {
        var max = await db.ConsolidatedPackages
            .Where(p => p.ProcedureId == procedureId && p.TenantId == tenantId)
            .Select(p => (int?)p.Version)
            .MaxAsync(ct);

        return max ?? 0;
    }

    public async Task CreateConsolidatedPackageAsync(ConsolidatedPackage package, CancellationToken ct = default)
    {
        db.ConsolidatedPackages.Add(package);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConsolidatedPackage>> GetConsolidatedPackagesAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        await db.ConsolidatedPackages
            .Where(p => p.ProcedureId == procedureId && p.TenantId == tenantId)
            .OrderByDescending(p => p.Version)
            .ToListAsync(ct);

    public Task<ConsolidatedPackage?> GetLatestConsolidatedPackageAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default) =>
        db.ConsolidatedPackages
            .Where(p => p.ProcedureId == procedureId && p.TenantId == tenantId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(ct);
}
