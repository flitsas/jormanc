using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Application.Queries;

/// <summary>
/// Stub HU-9788 — vendedores secundarios RUNT; integración completa en iteración posterior.
/// </summary>
public sealed class GetSecondarySellersQueryHandler(IProcedureRepository procedureRepository)
{
    public async Task<IReadOnlyList<SecondarySellerDto>> HandleAsync(
        GetSecondarySellersQuery query, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            query.ProcedureId, query.TenantId, ct);
        if (procedure is null)
            return [];

        return [];
    }
}
