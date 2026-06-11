namespace Flit.Modules.Procedures.Application.Commands;

/// <summary>AC3 HU-9786 — POST /procedures/{id}/attachments</summary>
public sealed record UploadAttachmentCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId,
    string LabelSlug,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
