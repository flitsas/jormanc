using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Interfaces;

namespace Flit.Modules.OT.Application.Queries;

/// <summary>
/// AC3 HU-9798 — GET /api/v1/ot-organisms filtra por tenant_id del JWT.
/// </summary>
public sealed class ListOtOrganismsQueryHandler(IOtOrganismRepository repository)
{
    public async Task<IReadOnlyList<OtOrganismDto>> HandleAsync(
        ListOtOrganismsQuery query, CancellationToken ct = default)
    {
        var organisms = await repository.ListByTenantAsync(query.TenantId, ct);
        return organisms.Select(OtOrganismMapper.ToDto).ToList();
    }
}
