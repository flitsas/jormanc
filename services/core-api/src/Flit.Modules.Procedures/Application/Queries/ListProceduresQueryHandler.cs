using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Mapping;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Application.Queries;

public sealed class ListProceduresQueryHandler(IProcedureRepository procedureRepository)
{
    public async Task<ProcedureListPageDto> HandleAsync(
        ListProceduresQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var filter = new ProcedureListFilter(
            TenantId: query.TenantId,
            Status: query.Status,
            DateFrom: query.DateFrom,
            Page: page,
            PageSize: pageSize);

        var (items, total) = await procedureRepository.ListAsync(filter, ct);

        return new ProcedureListPageDto(
            Data: items.Select(ProcedureMapper.ToListItem).ToList(),
            Total: total,
            Page: page,
            PageSize: pageSize);
    }
}
