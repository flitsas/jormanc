namespace Flit.Modules.OT.Application.DTOs;

public sealed record DocumentTypeSummaryDto(Guid Id, string Name);

public sealed record OrderedDocumentDto(int OrderIndex, DocumentTypeSummaryDto DocumentType);

public sealed record DocumentOrderEntryDto(
    Guid ProcedureTypeId,
    IReadOnlyList<OrderedDocumentDto> OrderedDocuments);

public sealed record UpdateDocumentOrderResultDto(
    Guid ProcedureTypeId,
    IReadOnlyList<OrderedDocumentDto> OrderedDocuments,
    bool Updated);

public sealed record OtDocumentLabelDto(
    Guid Id,
    Guid OtId,
    string Slug,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record OtLabelImpactDto(int ImpactCount);

public sealed record DeleteOtLabelResultDto(bool Deleted, int AffectedAttachments);
