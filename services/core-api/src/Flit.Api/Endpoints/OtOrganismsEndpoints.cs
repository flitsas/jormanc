using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Application.Queries;
using Flit.Modules.OT.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// CRUD de Organismos de Tránsito, prelación documental y etiquetas — HU-9798, HU-9799.
/// </summary>
public static class OtOrganismsEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";
    private const string ReadPermission = "ot.read";
    private const string ManagePermission = "ot.manage";

    public static IEndpointRouteBuilder MapOtOrganismsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ot-organisms").WithTags("OT");

        group.MapGet("/", HandleListAsync)
            .WithName("ListOtOrganisms")
            .RequireAuthorization()
            .Produces<OtOrganismResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapPost("/", HandleCreateAsync)
            .WithName("CreateOtOrganism")
            .RequireAuthorization()
            .Produces<OtOrganismResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", HandleGetByIdAsync)
            .WithName("GetOtOrganism")
            .RequireAuthorization()
            .Produces<OtOrganismResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", HandleUpdateAsync)
            .WithName("UpdateOtOrganism")
            .RequireAuthorization()
            .Produces<OtOrganismResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", HandleDeleteAsync)
            .WithName("DeleteOtOrganism")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/mode", HandleUpdateModeAsync)
            .WithName("UpdateOtOrganismMode")
            .RequireAuthorization()
            .Produces<OtOrganismResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/quipux-config", HandleUpdateQuipuxConfigAsync)
            .WithName("UpdateOtQuipuxConfig")
            .RequireAuthorization()
            .Produces<OtOrganismResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/document-order", HandleGetDocumentOrderAsync)
            .WithName("GetOtDocumentOrder")
            .RequireAuthorization()
            .Produces<DocumentOrderEntryResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/document-order/{procedureTypeId:guid}", HandleUpdateDocumentOrderAsync)
            .WithName("UpdateOtDocumentOrder")
            .RequireAuthorization()
            .Produces<UpdateDocumentOrderResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/labels", HandleListLabelsAsync)
            .WithName("ListOtLabels")
            .RequireAuthorization()
            .Produces<OtDocumentLabelResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/labels", HandleCreateLabelAsync)
            .WithName("CreateOtLabel")
            .RequireAuthorization()
            .Produces<OtDocumentLabelResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/labels/{labelId:guid}", HandleUpdateLabelAsync)
            .WithName("UpdateOtLabel")
            .RequireAuthorization()
            .Produces<OtDocumentLabelResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/labels/{labelId:guid}", HandleDeleteLabelAsync)
            .WithName("DeleteOtLabel")
            .RequireAuthorization()
            .Produces<DeleteOtLabelResponse>(StatusCodes.Status200OK)
            .Produces<LabelInUseErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/labels/{labelId:guid}/impact", HandleGetLabelImpactAsync)
            .WithName("GetOtLabelImpact")
            .RequireAuthorization()
            .Produces<OtLabelImpactResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/integration-logs", HandleGetIntegrationLogsAsync)
            .WithName("GetOtIntegrationLogs")
            .RequireAuthorization()
            .Produces<OtIntegrationLogsPageResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> HandleListAsync(
        HttpContext ctx,
        ListOtOrganismsQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var items = await handler.HandleAsync(new ListOtOrganismsQuery(tenantId), ct);
        return Results.Ok(items.Select(MapResponse).ToArray());
    }

    private static async Task<IResult> HandleCreateAsync(
        [FromBody] CreateOtOrganismRequest request,
        HttpContext ctx,
        CreateOtOrganismCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (!TryValidateCreate(request, out var errors))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", string.Join("; ", errors)));

        var command = new CreateOtOrganismCommand(tenantId, userId, request.Slug, request.Name);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/ot-organisms/{dto.Id}", MapResponse(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleGetByIdAsync(
        Guid id,
        HttpContext ctx,
        GetOtOrganismQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetOtOrganismQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(MapResponse(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleUpdateAsync(
        Guid id,
        [FromBody] UpdateOtOrganismRequest request,
        HttpContext ctx,
        UpdateOtOrganismCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "name es requerido."));

        var command = new UpdateOtOrganismCommand(id, tenantId, userId, request.Name);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(MapResponse(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleDeleteAsync(
        Guid id,
        HttpContext ctx,
        DeleteOtOrganismCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new DeleteOtOrganismCommand(id, tenantId, userId), ct);
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleUpdateModeAsync(
        Guid id,
        [FromBody] UpdateOtModeRequest request,
        HttpContext ctx,
        UpdateOtModeCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Mode))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "mode es requerido."));

        var command = new UpdateOtModeCommand(id, tenantId, userId, request.Mode);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(MapResponse(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleUpdateQuipuxConfigAsync(
        Guid id,
        [FromBody] UpdateQuipuxConfigRequest request,
        HttpContext ctx,
        UpdateQuipuxConfigCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "endpoint es requerido."));

        var command = new UpdateQuipuxConfigCommand(
            id, tenantId, userId, request.Endpoint, request.WebhookToken);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(MapResponse(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleGetDocumentOrderAsync(
        Guid id,
        HttpContext ctx,
        GetDocumentOrderQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetDocumentOrderQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: entries => Results.Ok(entries.Select(MapDocumentOrderEntry).ToArray()),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleUpdateDocumentOrderAsync(
        Guid id,
        Guid procedureTypeId,
        [FromBody] UpdateDocumentOrderRequest request,
        HttpContext ctx,
        UpdateDocumentOrderCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (request.OrderedDocumentTypeIds is null || request.OrderedDocumentTypeIds.Length == 0)
        {
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR", "ordered_document_type_ids es requerido."));
        }

        var sw = Stopwatch.StartNew();
        var command = new UpdateDocumentOrderCommand(
            id, procedureTypeId, tenantId, userId, request.OrderedDocumentTypeIds);
        var result = await handler.HandleAsync(command, ct);
        sw.Stop();
        ctx.Response.Headers["X-Response-Time"] = $"{sw.ElapsedMilliseconds}ms";

        return result.Match(
            onSuccess: dto => Results.Ok(new UpdateDocumentOrderResponse(
                dto.ProcedureTypeId,
                dto.OrderedDocuments.Select(MapOrderedDocument).ToArray(),
                dto.Updated,
                "Prelación actualizada")),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleListLabelsAsync(
        Guid id,
        HttpContext ctx,
        GetOtLabelsQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetOtLabelsQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: labels => Results.Ok(labels.Select(MapLabel).ToArray()),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleCreateLabelAsync(
        Guid id,
        [FromBody] CreateOtLabelRequest request,
        HttpContext ctx,
        CreateOtLabelCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR", "slug y display_name son requeridos."));
        }

        var command = new CreateOtLabelCommand(id, tenantId, userId, request.Slug, request.DisplayName);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/ot-organisms/{id}/labels/{dto.Id}", MapLabel(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleUpdateLabelAsync(
        Guid id,
        Guid labelId,
        [FromBody] UpdateOtLabelRequest request,
        HttpContext ctx,
        UpdateOtLabelCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        var command = new UpdateOtLabelCommand(
            id, labelId, tenantId, userId, request.DisplayName, request.IsActive);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(MapLabel(dto)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleDeleteLabelAsync(
        Guid id,
        Guid labelId,
        [FromBody] DeleteOtLabelRequest? request,
        HttpContext ctx,
        DeleteOtLabelCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, ManagePermission);
        if (forbidden is not null) return forbidden;

        var confirm = request?.Confirm ?? false;
        var command = new DeleteOtLabelCommand(id, labelId, tenantId, userId, confirm);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(new DeleteOtLabelResponse(dto.Deleted, dto.AffectedAttachments)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleGetLabelImpactAsync(
        Guid id,
        Guid labelId,
        HttpContext ctx,
        GetOtLabelImpactQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetOtLabelImpactQuery(id, labelId, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(new OtLabelImpactResponse(dto.ImpactCount)),
            onFailure: MapOtError);
    }

    private static async Task<IResult> HandleGetIntegrationLogsAsync(
        Guid id,
        HttpContext ctx,
        GetOtIntegrationLogsQueryHandler handler,
        [FromQuery(Name = "page")] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery(Name = "event_type")] string? eventType = null,
        [FromQuery(Name = "from")] DateTimeOffset? from = null,
        [FromQuery(Name = "to")] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var query = new GetOtIntegrationLogsQuery(id, tenantId, page, pageSize, eventType, from, to);
        var result = await handler.HandleAsync(query, ct);

        return result.Match(
            onSuccess: pageDto => Results.Ok(new OtIntegrationLogsPageResponse(
                pageDto.Data.Select(MapIntegrationLog).ToArray(),
                pageDto.Total,
                pageDto.Page,
                pageDto.PageSize)),
            onFailure: MapOtError);
    }

    private static OtOrganismResponse MapResponse(OtOrganismDto dto) =>
        new(dto.Id, dto.TenantId, dto.Slug, dto.Name, dto.Mode, dto.QuipuxEnabled,
            dto.QuipuxConfig, dto.CreatedAt, dto.UpdatedAt);

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

    private static DocumentOrderEntryResponse MapDocumentOrderEntry(DocumentOrderEntryDto dto) =>
        new(dto.ProcedureTypeId, dto.OrderedDocuments.Select(MapOrderedDocument).ToArray());

    private static OrderedDocumentResponse MapOrderedDocument(OrderedDocumentDto dto) =>
        new(dto.OrderIndex, new DocumentTypeSummaryResponse(dto.DocumentType.Id, dto.DocumentType.Name));

    private static OtDocumentLabelResponse MapLabel(OtDocumentLabelDto dto) =>
        new(dto.Id, dto.OtId, dto.Slug, dto.DisplayName, dto.IsActive, dto.CreatedAt);

    private static OtIntegrationLogResponse MapIntegrationLog(OtIntegrationLogDto dto) =>
        new(dto.EventType, dto.ProcedureRef, dto.HttpStatus, dto.DurationMs, dto.LoggedAt);

    private static IResult MapOtError(OtError err) => err.Code switch
    {
        "OT_SLUG_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "OT_LABEL_SLUG_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "LABEL_IN_USE" => Results.Conflict(new LabelInUseErrorResponse(err.Code, err.ImpactCount ?? 0)),
        "OT_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "OT_LABEL_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "OT_PROCEDURE_TYPE_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
    };

    private static bool TryValidateCreate(CreateOtOrganismRequest req, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(req.Slug)) errors.Add("slug es requerido.");
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name es requerido.");
        return errors.Count == 0;
    }
}

public sealed record CreateOtOrganismRequest(
    [property: Required] string Slug,
    [property: Required] string Name);

public sealed record UpdateOtOrganismRequest(
    [property: Required] string Name);

public sealed record UpdateOtModeRequest(
    [property: Required] string Mode);

public sealed record UpdateQuipuxConfigRequest(
    [property: Required] string Endpoint,
    string? WebhookToken);

public sealed record OtOrganismResponse(
    Guid Id,
    Guid TenantId,
    string Slug,
    string Name,
    string Mode,
    bool QuipuxEnabled,
    string? QuipuxConfig,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateDocumentOrderRequest(Guid[] OrderedDocumentTypeIds);

public sealed record DocumentTypeSummaryResponse(Guid Id, string Name);

public sealed record OrderedDocumentResponse(int OrderIndex, DocumentTypeSummaryResponse DocumentType);

public sealed record DocumentOrderEntryResponse(
    Guid ProcedureTypeId,
    OrderedDocumentResponse[] OrderedDocuments);

public sealed record UpdateDocumentOrderResponse(
    Guid ProcedureTypeId,
    OrderedDocumentResponse[] OrderedDocuments,
    bool Updated,
    string Message);

public sealed record CreateOtLabelRequest(
    [property: Required] string Slug,
    [property: Required] string DisplayName);

public sealed record UpdateOtLabelRequest(
    [property: Required] string DisplayName,
    bool? IsActive);

public sealed record DeleteOtLabelRequest(bool Confirm);

public sealed record OtDocumentLabelResponse(
    Guid Id,
    Guid OtId,
    string Slug,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record DeleteOtLabelResponse(bool Deleted, int AffectedAttachments);

public sealed record OtLabelImpactResponse(int ImpactCount);

public sealed record LabelInUseErrorResponse(string Error, int ImpactCount);

public sealed record OtIntegrationLogResponse(
    string EventType,
    string? ProcedureRef,
    int? HttpStatus,
    int? DurationMs,
    DateTimeOffset LoggedAt);

public sealed record OtIntegrationLogsPageResponse(
    OtIntegrationLogResponse[] Data,
    int Total,
    int Page,
    int PageSize);
