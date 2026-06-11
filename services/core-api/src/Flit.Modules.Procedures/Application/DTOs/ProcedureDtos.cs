using System.Text.Json.Serialization;

namespace Flit.Modules.Procedures.Application.DTOs;

public sealed record ProcedureStepSummaryDto(
    Guid Id,
    int OrderIndex,
    string Name,
    string StepType,
    bool IsRequired);

public sealed record ProcedureDto(
    Guid Id,
    string CompositeId,
    string Status,
    Guid ProcedureTypeId,
    Guid ProcedureTypeSnapshotId,
    Guid CompanyId,
    Guid? OtId,
    int CurrentStepOrder,
    IReadOnlyList<ProcedureStepSummaryDto> Steps,
    DateTimeOffset CreatedAt);

public sealed record VehicleSummaryDto(
    string? Plate,
    string? Vin,
    string? Brand,
    string? Model,
    int? Year,
    string? Color,
    string? OwnerName);

public sealed record CaptureVehicleResponseDto(
    Guid ProcedureId,
    string Status,
    VehicleSummaryDto Vehicle,
    IReadOnlyList<string> Warnings);

public sealed record ProcedureListItemDto(
    Guid Id,
    string CompositeId,
    string Status,
    Guid ProcedureTypeId,
    Guid CompanyId,
    DateTimeOffset CreatedAt);

public sealed record ProcedureListPageDto(
    IReadOnlyList<ProcedureListItemDto> Data,
    int Total,
    int Page,
    int PageSize);

public sealed record AddActorResponseDto(
    Guid ActorId,
    string Nature,
    object? QueryResults,
    IReadOnlyList<string> Warnings,
    Guid? LegalRepresentativeActorId = null);

public sealed record CuotaValidationErrorDto(
    string Error,
    [property: JsonPropertyName("current_sum")] decimal CurrentSum,
    [property: JsonPropertyName("proposed")] decimal Proposed);

public sealed record SubmitProcedureResponseDto(
    Guid Id,
    string Status,
    DateTimeOffset? SubmittedAt);

public sealed record FieldValidationErrorDto(
    [property: JsonPropertyName("field_slug")] string FieldSlug,
    string Step,
    string Error);

public sealed record SubmitValidationErrorsDto(
    IReadOnlyList<FieldValidationErrorDto> Errors);

public sealed record AttachmentResponseDto(
    [property: JsonPropertyName("attachment_id")] Guid AttachmentId,
    [property: JsonPropertyName("file_name")] string FileName,
    [property: JsonPropertyName("label_slug")] string LabelSlug);

public sealed record AttachmentListItemDto(
    Guid Id,
    string FileName,
    string LabelSlug,
    long SizeBytes,
    DateTimeOffset UploadedAt);

public sealed record SecondarySellerDto(
    string DocumentType,
    string DocumentNumber,
    string FullName,
    decimal? OwnershipPct);

public sealed record FormFieldConfigDto(
    Guid Id,
    Guid SectionId,
    int OrderIndex,
    string Slug,
    string Name,
    string FieldType,
    bool IsRequired,
    object? Config);

public sealed record FormSectionConfigDto(
    Guid Id,
    Guid StepId,
    int OrderIndex,
    string Slug,
    string Name,
    bool IsCollapsible,
    IReadOnlyList<FormFieldConfigDto> Fields);

public sealed record ProcedureStepConfigDto(
    Guid Id,
    int OrderIndex,
    string Name,
    string StepType,
    bool IsRequired,
    IReadOnlyList<FormSectionConfigDto> Sections);

public sealed record SnapshotConfigDto(
    IReadOnlyList<ProcedureStepConfigDto> Steps);

public sealed record ProcedureDetailDto(
    Guid Id,
    string CompositeId,
    string Status,
    Guid ProcedureTypeSnapshotId,
    string VehicleQueryKey,
    int CurrentStepOrder,
    object StepData,
    SnapshotConfigDto SnapshotConfig);
