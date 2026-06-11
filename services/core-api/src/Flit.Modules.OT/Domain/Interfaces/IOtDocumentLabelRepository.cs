using Flit.Infrastructure.Persistence.Entities.OT;

namespace Flit.Modules.OT.Domain.Interfaces;

public interface IOtDocumentLabelRepository
{
    Task<bool> SlugExistsAsync(
        Guid otId, string slug, Guid tenantId, CancellationToken ct = default);

    Task<OtDocumentLabel?> FindByIdAsync(
        Guid id, Guid otId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<OtDocumentLabel>> ListByOtAsync(
        Guid otId, Guid tenantId, CancellationToken ct = default);

    Task CreateAsync(OtDocumentLabel label, CancellationToken ct = default);

    Task UpdateAsync(OtDocumentLabel label, CancellationToken ct = default);

    Task DeleteAsync(OtDocumentLabel label, CancellationToken ct = default);

    Task<int> CountAttachmentImpactAsync(
        Guid otId, string labelSlug, Guid tenantId, CancellationToken ct = default);
}
