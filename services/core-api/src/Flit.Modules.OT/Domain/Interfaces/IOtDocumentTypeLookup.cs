using Flit.Infrastructure.Persistence.Entities.Documents;

namespace Flit.Modules.OT.Domain.Interfaces;

public interface IOtDocumentTypeLookup
{
    Task<bool> ProcedureTypeExistsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, DocumentType>> GetByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
