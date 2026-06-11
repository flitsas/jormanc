using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Queries;

/// <summary>AC3 HU-9789 — GET /procedure-types/{id}/document-config</summary>
public sealed class GetProcedureTypeDocumentConfigQueryHandler(IDocumentRepository repository)
{
    public async Task<Result<IReadOnlyList<ProcedureTypeDocumentConfigDto>, DocumentError>> HandleAsync(
        GetProcedureTypeDocumentConfigQuery query, CancellationToken ct = default)
    {
        if (!await repository.ProcedureTypeExistsAsync(query.ProcedureTypeId, query.TenantId, ct))
            return Result<IReadOnlyList<ProcedureTypeDocumentConfigDto>, DocumentError>.Failure(
                DocumentError.ProcedureTypeNotFound);

        var associations = await repository.GetAssociationsAsync(query.ProcedureTypeId, query.TenantId, ct);

        var dtos = associations
            .Select(a => AssociateDocumentToProcedureTypeCommandHandler.MapToDto(a))
            .ToList();

        return Result<IReadOnlyList<ProcedureTypeDocumentConfigDto>, DocumentError>.Success(dtos);
    }
}
