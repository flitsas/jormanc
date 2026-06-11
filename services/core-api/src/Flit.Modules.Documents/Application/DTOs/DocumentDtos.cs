namespace Flit.Modules.Documents.Application.DTOs;

public sealed record DocumentTypeDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string LoadType,
    string AllowedFormats,
    int MaxSizeMb,
    bool IsReusable,
    DateTimeOffset CreatedAt);

public sealed record ProcedureTypeDocumentConfigDto(
    Guid Id,
    Guid ProcedureTypeId,
    Guid DocumentTypeId,
    string DocumentTypeName,
    string LoadType,
    bool IsRequired,
    int OrderIndex,
    Guid? ActorDefinitionId,
    bool AllowPartialConsolidation,
    DateTimeOffset CreatedAt);

public sealed record DocumentTemplateDto(
    Guid TemplateId,
    Guid DocumentTypeId,
    int Version,
    string Status,
    string ContentRef,
    string? Notes,
    IReadOnlyList<string> MarkersDetected,
    DateTimeOffset CreatedAt);

public sealed record DocumentTemplateDetailDto(
    Guid TemplateId,
    Guid DocumentTypeId,
    int Version,
    string Status,
    string ContentRef,
    string HtmlContent,
    IReadOnlyList<string> MarkersDetected,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record ProcedureDocumentItemDto(
    Guid Id,
    Guid DocumentTypeId,
    string DocumentTypeName,
    string LoadType,
    string Origin,
    string Status,
    int? TemplateVersion,
    string? FileRef,
    DateTimeOffset? GeneratedAt,
    Guid? UploadedBy,
    bool IsRequired,
    int OrderIndex);

public sealed record ConsolidatedPackageDto(
    int Version,
    string MergedFileRef,
    DateTimeOffset CreatedAt,
    string DownloadFilename,
    int DocCount);

public sealed record ProcedureDocumentsStatusDto(
    Guid ProcedureId,
    IReadOnlyList<ProcedureDocumentItemDto> Documents,
    IReadOnlyList<ConsolidatedPackageDto> ConsolidatedPackages);
