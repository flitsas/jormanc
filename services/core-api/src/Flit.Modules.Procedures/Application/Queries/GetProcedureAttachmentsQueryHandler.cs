using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Application.Queries;

public sealed class GetProcedureAttachmentsQueryHandler(IProcedureRepository procedureRepository)
{
    public async Task<IReadOnlyList<AttachmentListItemDto>> HandleAsync(
        GetProcedureAttachmentsQuery query, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            query.ProcedureId, query.TenantId, ct);
        if (procedure is null)
            return [];

        var attachments = await procedureRepository.GetAttachmentsAsync(
            query.ProcedureId, query.TenantId, ct);

        return attachments
            .Select(a => new AttachmentListItemDto(
                a.Id,
                a.FileName,
                a.LabelSlug,
                a.SizeBytes,
                a.UploadedAt))
            .ToList();
    }
}
