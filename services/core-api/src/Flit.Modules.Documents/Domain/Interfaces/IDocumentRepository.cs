using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;

namespace Flit.Modules.Documents.Domain.Interfaces;

public interface IDocumentRepository
{
    Task CreateDocumentTypeAsync(DocumentType documentType, CancellationToken ct = default);

    Task<DocumentType?> FindDocumentTypeByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<bool> ProcedureTypeExistsAsync(Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<bool> AssociationExistsAsync(
        Guid procedureTypeId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task CreateAssociationAsync(ProcedureTypeDocument association, CancellationToken ct = default);

    Task<IReadOnlyList<ProcedureTypeDocument>> GetAssociationsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<bool> ActorDefinitionBelongsToProcedureTypeAsync(
        Guid actorDefinitionId, Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<int> GetMaxTemplateVersionAsync(Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task<DocumentTemplate?> FindActiveTemplateAsync(Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task CreateTemplateAsync(
        DocumentTemplate documentTemplate, IReadOnlyList<TemplateField> fields, CancellationToken ct = default);

    Task DeprecateActiveTemplatesAsync(Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentTemplate>> GetTemplatesAsync(
        Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task<DocumentTemplate?> FindTemplateByIdAsync(
        Guid templateId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task<Procedure?> FindProcedureAsync(Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<ProcedureActor>> GetProcedureActorsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<ActorDefinition>> GetActorDefinitionsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<VehicleQuery?> GetLatestVehicleQueryAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task<OtOrganism?> FindOtAsync(Guid otId, Guid tenantId, CancellationToken ct = default);

    Task<ProcedureDocument?> FindProcedureDocumentAsync(
        Guid procedureId, Guid documentTypeId, Guid tenantId, CancellationToken ct = default);

    Task CreateProcedureDocumentAsync(ProcedureDocument document, CancellationToken ct = default);

    Task UpdateProcedureDocumentAsync(ProcedureDocument document, CancellationToken ct = default);

    Task<IReadOnlyList<ProcedureDocument>> GetProcedureDocumentsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task<int> GetMaxConsolidatedVersionAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task CreateConsolidatedPackageAsync(ConsolidatedPackage package, CancellationToken ct = default);

    Task<IReadOnlyList<ConsolidatedPackage>> GetConsolidatedPackagesAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);

    Task<ConsolidatedPackage?> GetLatestConsolidatedPackageAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);
}
