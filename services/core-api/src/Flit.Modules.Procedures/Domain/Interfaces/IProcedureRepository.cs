using Flit.Infrastructure.Persistence.Entities.Procedures;

namespace Flit.Modules.Procedures.Domain.Interfaces;

public sealed record ProcedureListFilter(
    Guid TenantId,
    string? Status = null,
    DateTimeOffset? DateFrom = null,
    int Page = 1,
    int PageSize = 20);

public interface IProcedureRepository
{
    Task CreateAsync(Procedure procedure, CancellationToken ct = default);

    Task<Procedure?> FindByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<Procedure?> FindByCompositeIdAsync(string compositeId, Guid tenantId, CancellationToken ct = default);

    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);

    Task<(IReadOnlyList<Procedure> Items, int Total)> ListAsync(
        ProcedureListFilter filter, CancellationToken ct = default);

    Task AddVehicleQueryAsync(VehicleQuery vehicleQuery, CancellationToken ct = default);

    Task AddActorsAsync(IReadOnlyList<ProcedureActor> actors, CancellationToken ct = default);

    Task<IReadOnlyList<ProcedureActor>> GetActorsAsync(
        Guid procedureId, Guid tenantId, Guid? actorDefinitionId = null, CancellationToken ct = default);

    Task UpdateAsync(Procedure procedure, CancellationToken ct = default);

    Task AddAttachmentAsync(ProcedureAttachment attachment, CancellationToken ct = default);

    Task<IReadOnlyList<ProcedureAttachment>> GetAttachmentsAsync(
        Guid procedureId, Guid tenantId, CancellationToken ct = default);
}
