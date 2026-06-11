using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.OT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.OT.Infrastructure.Persistence;

/// <summary>Consulta tipos de documento del tenant para enriquecer respuestas de prelación.</summary>
public sealed class OtDocumentTypeLookup(FlitDbContext db) : IOtDocumentTypeLookup
{
    public Task<bool> ProcedureTypeExistsAsync(
        Guid procedureTypeId, Guid tenantId, CancellationToken ct = default) =>
        db.ProcedureTypes.AnyAsync(
            p => p.Id == procedureTypeId && p.TenantId == tenantId && p.DeletedAt == null,
            ct);

    public async Task<IReadOnlyDictionary<Guid, DocumentType>> GetByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, DocumentType>();

        var types = await db.DocumentTypes
            .Where(d => d.TenantId == tenantId && ids.Contains(d.Id) && d.DeletedAt == null)
            .ToListAsync(ct);

        return types.ToDictionary(d => d.Id);
    }
}
