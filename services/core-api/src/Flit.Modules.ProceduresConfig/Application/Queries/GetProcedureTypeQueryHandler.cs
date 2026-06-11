using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Queries;

/// <summary>AC1 HU-9779 — GET /procedure-types/{id} con estructura anidada</summary>
public sealed class GetProcedureTypeQueryHandler(IProcedureTypeRepository repository)
{
    public async Task<Result<ProcedureTypeDto, ProcedureTypeError>> HandleAsync(
        GetProcedureTypeQuery query, CancellationToken ct = default)
    {
        var entity = await repository.FindByIdWithDetailsAsync(query.Id, query.TenantId, ct);
        if (entity is null)
            return Result<ProcedureTypeDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        return Result<ProcedureTypeDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToDto(entity));
    }
}
