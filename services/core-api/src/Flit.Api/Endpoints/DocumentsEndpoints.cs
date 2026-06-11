using System.ComponentModel.DataAnnotations;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Application.Queries;
using ConsolidatedPackageDto = Flit.Modules.Documents.Application.DTOs.ConsolidatedPackageDto;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Maestro documental — HU-9789, HU-9790, HU-9791.
/// POST /document-types
/// POST|GET /procedure-types/{id}/document-config
/// POST|GET /document-types/{id}/templates
/// GET /procedures/{id}/documents
/// GET /procedures/{id}/consolidated
/// POST /procedures/{id}/documents/consolidate
/// </summary>
public static class DocumentsEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";
    private const string ReadPermission = "tramites.read";
    private const string ConsolidatePermission = "tramites.admin.maestro";

    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var documentTypes = app.MapGroup("/api/v1/document-types").WithTags("Documents");

        documentTypes.MapPost("/", HandleCreateDocumentTypeAsync)
            .WithName("CreateDocumentType")
            .RequireAuthorization()
            .Produces<DocumentTypeResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        documentTypes.MapPost("/{id:guid}/templates", HandleCreateDocumentTemplateAsync)
            .WithName("CreateDocumentTemplate")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<DocumentTemplateResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        documentTypes.MapGet("/{id:guid}/templates", HandleListDocumentTemplatesAsync)
            .WithName("ListDocumentTemplates")
            .RequireAuthorization()
            .Produces<DocumentTemplateResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        documentTypes.MapGet("/{id:guid}/templates/{versionId:guid}", HandleGetDocumentTemplateAsync)
            .WithName("GetDocumentTemplate")
            .RequireAuthorization()
            .Produces<DocumentTemplateDetailResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        documentTypes.MapPost("/{id:guid}/templates/{versionId:guid}/preview-pdf", HandlePreviewTemplatePdfAsync)
            .WithName("PreviewDocumentTemplatePdf")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        var procedureTypeDocs = app.MapGroup("/api/v1/procedure-types").WithTags("Documents");

        procedureTypeDocs.MapPost("/{id:guid}/document-config", HandleAssociateDocumentAsync)
            .WithName("AssociateDocumentToProcedureType")
            .RequireAuthorization()
            .Produces<ProcedureTypeDocumentConfigResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        procedureTypeDocs.MapGet("/{id:guid}/document-config", HandleGetDocumentConfigAsync)
            .WithName("GetProcedureTypeDocumentConfig")
            .RequireAuthorization()
            .Produces<ProcedureTypeDocumentConfigResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        var procedureDocs = app.MapGroup("/api/v1/procedures").WithTags("Documents");

        procedureDocs.MapGet("/{id:guid}/documents", HandleGetProcedureDocumentsAsync)
            .WithName("GetProcedureDocuments")
            .RequireAuthorization()
            .Produces<ProcedureDocumentsStatusResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        procedureDocs.MapGet("/{id:guid}/consolidated", HandleDownloadConsolidatedAsync)
            .WithName("DownloadConsolidatedPackage")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        procedureDocs.MapPost("/{id:guid}/documents/consolidate", HandleForceReconsolidationAsync)
            .WithName("ForceProcedureDocumentsConsolidation")
            .RequireAuthorization()
            .Produces<ConsolidatedPackageResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> HandleCreateDocumentTypeAsync(
        [FromBody] CreateDocumentTypeRequest request,
        HttpContext ctx,
        CreateDocumentTypeCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "name es requerido."));

        if (string.IsNullOrWhiteSpace(request.LoadType))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "load_type es requerido."));

        var allowedFormats = request.AllowedFormats is null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(request.AllowedFormats);

        var command = new CreateDocumentTypeCommand(
            tenantId,
            userId,
            request.Name,
            request.LoadType,
            allowedFormats,
            request.MaxSizeMb,
            request.IsReusable);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/document-types/{dto.Id}",
                MapDocumentType(dto)),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleAssociateDocumentAsync(
        Guid id,
        [FromBody] AssociateDocumentRequest request,
        HttpContext ctx,
        AssociateDocumentToProcedureTypeCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (request.DocumentTypeId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "document_type_id es requerido."));

        var command = new AssociateDocumentToProcedureTypeCommand(
            id,
            tenantId,
            userId,
            request.DocumentTypeId,
            request.IsRequired ?? true,
            request.OrderIndex ?? 0,
            request.ActorDefinitionId,
            request.AllowPartialConsolidation ?? false);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/procedure-types/{id}/document-config/{dto.Id}",
                MapDocumentConfig(dto)),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleGetDocumentConfigAsync(
        Guid id,
        HttpContext ctx,
        GetProcedureTypeDocumentConfigQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetProcedureTypeDocumentConfigQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapDocumentConfig).ToArray()),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleCreateDocumentTemplateAsync(
        Guid id,
        HttpRequest request,
        HttpContext ctx,
        CreateDocumentTemplateCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        if (!request.HasFormContentType)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "Se requiere multipart/form-data."));

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("html_content");
        if (file is null || file.Length == 0)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "html_content es requerido."));

        form.TryGetValue("notes", out var notesValue);
        var notes = notesValue.ToString();

        await using var stream = file.OpenReadStream();
        var command = new CreateDocumentTemplateCommand(id, tenantId, userId, stream, notes);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/document-types/{id}/templates/{dto.TemplateId}",
                MapDocumentTemplate(dto)),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleListDocumentTemplatesAsync(
        Guid id,
        HttpContext ctx,
        ListDocumentTemplatesQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new ListDocumentTemplatesQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapDocumentTemplate).ToArray()),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleGetDocumentTemplateAsync(
        Guid id,
        Guid versionId,
        HttpContext ctx,
        GetDocumentTemplateQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetDocumentTemplateQuery(id, versionId, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapDocumentTemplateDetail(dto)),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandlePreviewTemplatePdfAsync(
        Guid id,
        Guid versionId,
        [FromBody] TemplatePreviewContextRequest? body,
        HttpContext ctx,
        GenerateTemplatePreviewPdfCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractDocumentAdminClaims(ctx);
        if (forbidden is not null) return forbidden;

        var context = MapPreviewContext(body);
        var result = await handler.HandleAsync(
            new GenerateTemplatePreviewPdfCommand(id, versionId, tenantId, context), ct);

        return result.Match(
            onSuccess: pdf => Results.File(pdf, "application/pdf", $"template-preview-{versionId}.pdf"),
            onFailure: MapDocumentError);
    }

    private static TemplateContext MapPreviewContext(TemplatePreviewContextRequest? body)
    {
        if (body is null) return new TemplateContext();

        var actors = body.Actors?.ToDictionary(
            kvp => kvp.Key,
            kvp => new ActorContextData
            {
                FullName = kvp.Value.FullName,
                DocumentNumber = kvp.Value.DocumentNumber,
                Nit = kvp.Value.Nit,
                CuotaPct = kvp.Value.CuotaPct
            },
            StringComparer.Ordinal) ?? new Dictionary<string, ActorContextData>(StringComparer.Ordinal);

        return new TemplateContext
        {
            Procedure = new ProcedureContextData
            {
                CompositeId = body.Procedure?.CompositeId,
                SubmittedAt = body.Procedure?.SubmittedAt
            },
            Actors = actors,
            Vehicle = body.Vehicle is null ? null : new VehicleContextData
            {
                Plate = body.Vehicle.Plate,
                Runt = body.Vehicle.Runt ?? new Dictionary<string, string?>(),
                Simit = body.Vehicle.Simit ?? new Dictionary<string, string?>()
            },
            Identity = body.Identity is null ? null : new IdentityContextData
            {
                Verdict = body.Identity.Verdict,
                ValidatedAt = body.Identity.ValidatedAt
            },
            Ot = body.Ot is null ? null : new OtContextData
            {
                Name = body.Ot.Name,
                Code = body.Ot.Code
            }
        };
    }

    private static async Task<IResult> HandleGetProcedureDocumentsAsync(
        Guid id,
        HttpContext ctx,
        GetProcedureDocumentsQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetProcedureDocumentsQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapProcedureDocumentsStatus(dto)),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleDownloadConsolidatedAsync(
        Guid id,
        HttpContext ctx,
        GetConsolidatedPackageQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetConsolidatedPackageQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: tuple => Results.File(tuple.Content, "application/pdf", tuple.Filename),
            onFailure: MapDocumentError);
    }

    private static async Task<IResult> HandleForceReconsolidationAsync(
        Guid id,
        HttpContext ctx,
        ForceReconsolidationCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ConsolidatePermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(
            new ForceReconsolidationCommand(id, tenantId, userId), ct);

        return result.Match(
            onSuccess: dto => Results.Ok(MapConsolidatedPackage(dto)),
            onFailure: MapDocumentError);
    }

    private static (Guid TenantId, Guid UserId, IResult? Error) ExtractTenantClaims(
        HttpContext ctx, string requiredPermission)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized));
        }

        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token sin tenant_id."),
                statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        var perms = ctx.User.FindAll("perms").Select(c => c.Value).ToHashSet();

        if (!roles.Contains(SuperAdminRoleClaim) && !perms.Contains(requiredPermission))
        {
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("FORBIDDEN", $"Se requiere permiso {requiredPermission}."),
                statusCode: StatusCodes.Status403Forbidden));
        }

        return (tenantId, userId, null);
    }

    private static (Guid TenantId, Guid UserId, IResult? Error) ExtractDocumentAdminClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty,
                Results.Json(new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                    statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        if (!roles.Contains(SuperAdminRoleClaim))
        {
            return (Guid.Empty, Guid.Empty,
                Results.Json(new ErrorResponse("FORBIDDEN", "Se requiere el rol superadmin."),
                    statusCode: StatusCodes.Status403Forbidden));
        }

        return (tenantId, userId, null);
    }

    private static IResult MapDocumentError(DocumentError err) => err.Code switch
    {
        "DOCUMENT_TYPE_NOT_FOUND" or "PROCEDURE_TYPE_NOT_FOUND" or "ACTOR_DEFINITION_NOT_FOUND"
            or "DOCUMENT_TEMPLATE_NOT_FOUND" or "PROCEDURE_NOT_FOUND" or "CONSOLIDATED_PACKAGE_NOT_FOUND" =>
            Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "CONSOLIDATION_NOT_READY" =>
            Results.BadRequest(new ErrorResponse(err.Code, err.Message)),
        "DOCUMENT_ALREADY_ASSOCIATED" =>
            Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
    };

    private static DocumentTemplateResponse MapDocumentTemplate(DocumentTemplateDto dto) =>
        new(
            dto.TemplateId,
            dto.DocumentTypeId,
            dto.Version,
            dto.Status,
            dto.ContentRef,
            dto.Notes,
            dto.MarkersDetected.ToArray(),
            dto.CreatedAt);

    private static DocumentTemplateDetailResponse MapDocumentTemplateDetail(DocumentTemplateDetailDto dto) =>
        new(
            dto.TemplateId,
            dto.DocumentTypeId,
            dto.Version,
            dto.Status,
            dto.ContentRef,
            dto.HtmlContent,
            dto.MarkersDetected.ToArray(),
            dto.Notes,
            dto.CreatedAt);

    private static DocumentTypeResponse MapDocumentType(DocumentTypeDto dto) =>
        new(dto.Id, dto.TenantId, dto.Name, dto.LoadType, dto.MaxSizeMb, dto.IsReusable, dto.CreatedAt);

    private static ProcedureTypeDocumentConfigResponse MapDocumentConfig(ProcedureTypeDocumentConfigDto dto) =>
        new(
            dto.Id,
            dto.ProcedureTypeId,
            dto.DocumentTypeId,
            dto.DocumentTypeName,
            dto.LoadType,
            dto.IsRequired,
            dto.OrderIndex,
            dto.ActorDefinitionId,
            dto.AllowPartialConsolidation,
            dto.CreatedAt);

    private static ProcedureDocumentsStatusResponse MapProcedureDocumentsStatus(ProcedureDocumentsStatusDto dto) =>
        new(
            dto.ProcedureId,
            dto.Documents.Select(MapProcedureDocumentItem).ToArray(),
            dto.ConsolidatedPackages.Select(MapConsolidatedPackage).ToArray());

    private static ProcedureDocumentItemResponse MapProcedureDocumentItem(ProcedureDocumentItemDto dto) =>
        new(
            dto.Id,
            new ProcedureDocumentTypeResponse(dto.DocumentTypeId, dto.DocumentTypeName, dto.LoadType),
            dto.Origin,
            dto.Status,
            dto.TemplateVersion,
            dto.FileRef,
            dto.GeneratedAt,
            dto.UploadedBy,
            dto.IsRequired,
            dto.OrderIndex);

    private static ConsolidatedPackageResponse MapConsolidatedPackage(ConsolidatedPackageDto dto) =>
        new(dto.Version, dto.MergedFileRef, dto.CreatedAt, dto.DownloadFilename, dto.DocCount);
}

public sealed record CreateDocumentTypeRequest(
    [property: Required] string Name,
    [property: Required] string LoadType,
    string[]? AllowedFormats,
    int? MaxSizeMb,
    bool? IsReusable);

public sealed record AssociateDocumentRequest(
    [property: Required] Guid DocumentTypeId,
    bool? IsRequired,
    int? OrderIndex,
    Guid? ActorDefinitionId,
    bool? AllowPartialConsolidation);

public sealed record DocumentTypeResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string LoadType,
    int MaxSizeMb,
    bool IsReusable,
    DateTimeOffset CreatedAt);

public sealed record ProcedureTypeDocumentConfigResponse(
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

public sealed record DocumentTemplateResponse(
    Guid TemplateId,
    Guid DocumentTypeId,
    int Version,
    string Status,
    string ContentRef,
    string? Notes,
    string[] MarkersDetected,
    DateTimeOffset CreatedAt);

public sealed record DocumentTemplateDetailResponse(
    Guid TemplateId,
    Guid DocumentTypeId,
    int Version,
    string Status,
    string ContentRef,
    string HtmlContent,
    string[] MarkersDetected,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record TemplatePreviewContextRequest(
    TemplatePreviewProcedureRequest? Procedure,
    Dictionary<string, TemplatePreviewActorRequest>? Actors,
    TemplatePreviewVehicleRequest? Vehicle,
    TemplatePreviewIdentityRequest? Identity,
    TemplatePreviewOtRequest? Ot);

public sealed record TemplatePreviewProcedureRequest(string? CompositeId, DateTimeOffset? SubmittedAt);

public sealed record TemplatePreviewActorRequest(
    string? FullName,
    string? DocumentNumber,
    string? Nit,
    decimal? CuotaPct);

public sealed record TemplatePreviewVehicleRequest(
    string? Plate,
    Dictionary<string, string?>? Runt,
    Dictionary<string, string?>? Simit);

public sealed record TemplatePreviewIdentityRequest(string? Verdict, DateTimeOffset? ValidatedAt);

public sealed record TemplatePreviewOtRequest(string? Name, string? Code);

public sealed record ProcedureDocumentsStatusResponse(
    Guid ProcedureId,
    ProcedureDocumentItemResponse[] Documents,
    ConsolidatedPackageResponse[] ConsolidatedPackages);

public sealed record ProcedureDocumentItemResponse(
    Guid Id,
    ProcedureDocumentTypeResponse DocumentType,
    string Origin,
    string Status,
    int? TemplateVersion,
    string? FileRef,
    DateTimeOffset? GeneratedAt,
    Guid? UploadedBy,
    bool IsRequired,
    int OrderIndex);

public sealed record ProcedureDocumentTypeResponse(Guid Id, string Name, string LoadType);

public sealed record ConsolidatedPackageResponse(
    int Version,
    string MergedFileRef,
    DateTimeOffset CreatedAt,
    string DownloadFilename,
    int DocCount);
