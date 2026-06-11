using Flit.Infrastructure.Persistence.Entities.OT;

namespace Flit.Modules.OT.Domain.Interfaces;

public interface IOtDocumentOrderRepository
{
    Task<OtDocumentOrder?> FindByOtAndProcedureTypeAsync(
        Guid otId, Guid procedureTypeId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<OtDocumentOrder>> ListByOtAsync(
        Guid otId, Guid tenantId, CancellationToken ct = default);

    Task UpsertAsync(OtDocumentOrder order, CancellationToken ct = default);
}
