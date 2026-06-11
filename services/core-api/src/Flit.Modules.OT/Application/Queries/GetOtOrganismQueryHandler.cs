using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Queries;

public sealed class GetOtOrganismQueryHandler(IOtOrganismRepository repository)
{
    public async Task<Result<OtOrganismDto, OtError>> HandleAsync(
        GetOtOrganismQuery query, CancellationToken ct = default)
    {
        var organism = await repository.FindByIdAsync(query.Id, query.TenantId, ct);
        if (organism is null)
            return Result<OtOrganismDto, OtError>.Failure(OtError.NotFound);

        return Result<OtOrganismDto, OtError>.Success(OtOrganismMapper.ToDto(organism));
    }
}
