using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Domain.Errors;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Procedures.Application.Commands;

public sealed class UploadAttachmentCommandHandler(
    IProcedureRepository procedureRepository,
    IFileStorage fileStorage,
    IClock clock)
{
    private static readonly HashSet<string> AllowedStatuses = ["draft", "submitted"];

    public async Task<Result<AttachmentResponseDto, ProcedureError>> HandleAsync(
        UploadAttachmentCommand command, CancellationToken ct = default)
    {
        var procedure = await procedureRepository.FindByIdAsync(
            command.ProcedureId, command.TenantId, ct);
        if (procedure is null)
            return Result<AttachmentResponseDto, ProcedureError>.Failure(ProcedureError.NotFound);

        if (!AllowedStatuses.Contains(procedure.Status))
            return Result<AttachmentResponseDto, ProcedureError>.Failure(ProcedureError.InvalidAttachmentStatus);

        if (string.IsNullOrWhiteSpace(command.LabelSlug) || string.IsNullOrWhiteSpace(command.FileName))
            return Result<AttachmentResponseDto, ProcedureError>.Failure(ProcedureError.AttachmentUploadFailed);

        var objectKey =
            $"procedures/{command.TenantId}/{command.ProcedureId}/{command.LabelSlug.Trim()}/{command.FileName.Trim()}";

        try
        {
            await fileStorage.UploadAsync(objectKey, command.Content, command.ContentType, ct);
        }
        catch (IOException)
        {
            return Result<AttachmentResponseDto, ProcedureError>.Failure(ProcedureError.AttachmentUploadFailed);
        }

        var now = clock.UtcNow;
        var attachment = new ProcedureAttachment
        {
            Id = Guid.NewGuid(),
            ProcedureId = command.ProcedureId,
            TenantId = command.TenantId,
            LabelSlug = command.LabelSlug.Trim(),
            FileName = command.FileName.Trim(),
            FileRef = objectKey,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            UploadedBy = command.UserId,
            UploadedAt = now
        };

        await procedureRepository.AddAttachmentAsync(attachment, ct);

        return Result<AttachmentResponseDto, ProcedureError>.Success(
            new AttachmentResponseDto(attachment.Id, attachment.FileName, attachment.LabelSlug));
    }
}
