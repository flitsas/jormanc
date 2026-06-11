using System.ComponentModel.DataAnnotations;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Application.Queries;
using Flit.Modules.Procedures.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints de creación y consulta de trámites — HU-9784.
/// POST   /api/v1/procedures
/// PATCH  /api/v1/procedures/{id}/vehicle
/// POST   /api/v1/procedures/{id}/actors
/// GET    /api/v1/procedures
/// GET    /api/v1/procedures/{id}
/// </summary>
public static class ProceduresEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";
    private const string CreatePermission = "tramites.create";
    private const string ReadPermission = "tramites.read";

    public static IEndpointRouteBuilder MapProceduresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/procedures").WithTags("Procedures");

        group.MapPost("/", HandleCreateProcedureAsync)
            .WithName("CreateProcedure")
            .RequireAuthorization()
            .Produces<ProcedureDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/vehicle", HandleCaptureVehicleAsync)
            .WithName("CaptureVehicle")
            .RequireAuthorization()
            .Produces<CaptureVehicleResponseDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/actors", HandleAddActorAsync)
            .WithName("AddProcedureActor")
            .RequireAuthorization()
            .Produces<AddActorResponseDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<CuotaValidationErrorDto>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/submit", HandleSubmitProcedureAsync)
            .WithName("SubmitProcedure")
            .RequireAuthorization()
            .Produces<SubmitProcedureResponseDto>(StatusCodes.Status200OK)
            .Produces<SubmitValidationErrorsDto>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/attachments", HandleUploadAttachmentAsync)
            .WithName("UploadProcedureAttachment")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<AttachmentResponseDto>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/attachments", HandleListAttachmentsAsync)
            .WithName("ListProcedureAttachments")
            .RequireAuthorization()
            .Produces<IReadOnlyList<AttachmentListItemDto>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/secondary-sellers", HandleGetSecondarySellersAsync)
            .WithName("GetProcedureSecondarySellers")
            .RequireAuthorization()
            .Produces<IReadOnlyList<SecondarySellerDto>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/", HandleListProceduresAsync)
            .WithName("ListProcedures")
            .RequireAuthorization()
            .Produces<ProcedureListPageDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", HandleGetProcedureDetailAsync)
            .WithName("GetProcedureDetail")
            .RequireAuthorization()
            .Produces<ProcedureDetailDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> HandleCreateProcedureAsync(
        [FromBody] CreateProcedureRequest request,
        HttpContext ctx,
        CreateProcedureCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, CreatePermission);
        if (forbidden is not null) return forbidden;

        if (request.ProcedureTypeId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "procedure_type_id es requerido."));
        if (request.CompanyId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "company_id es requerido."));

        var command = new CreateProcedureCommand(
            TenantId: tenantId,
            UserId: userId,
            ProcedureTypeId: request.ProcedureTypeId,
            CompanyId: request.CompanyId,
            OtId: request.OtId);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/procedures/{dto.Id}", dto),
            onFailure: MapProcedureError);
    }

    private static async Task<IResult> HandleCaptureVehicleAsync(
        Guid id,
        [FromBody] CaptureVehicleRequest request,
        HttpContext ctx,
        CaptureVehicleCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, CreatePermission);
        if (forbidden is not null) return forbidden;

        if (string.IsNullOrWhiteSpace(request.Plate) && string.IsNullOrWhiteSpace(request.Vin))
            return Results.BadRequest(
                new ErrorResponse("VALIDATION_ERROR", "plate o vin es requerido."));

        var command = new CaptureVehicleCommand(
            ProcedureId: id,
            TenantId: tenantId,
            UserId: userId,
            Plate: request.Plate,
            Vin: request.Vin);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Ok(dto),
            onFailure: MapProcedureError);
    }

    private static async Task<IResult> HandleAddActorAsync(
        Guid id,
        [FromBody] AddActorRequest request,
        HttpContext ctx,
        AddActorCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, CreatePermission);
        if (forbidden is not null) return forbidden;

        if (request.ActorDefinitionId == Guid.Empty)
            return Results.BadRequest(
                new ErrorResponse("VALIDATION_ERROR", "actor_definition_id es requerido."));
        if (string.IsNullOrWhiteSpace(request.Nature))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "nature es requerido."));

        var command = new AddActorCommand(
            ProcedureId: id,
            TenantId: tenantId,
            UserId: userId,
            ActorDefinitionId: request.ActorDefinitionId,
            Nature: request.Nature.Trim().ToLowerInvariant(),
            DocumentType: request.DocumentType,
            DocumentNumber: request.DocumentNumber,
            Nit: request.Nit,
            CuotaPct: request.CuotaPct);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/procedures/{id}/actors/{dto.ActorId}", dto),
            onFailure: MapProcedureError);
    }

    private static async Task<IResult> HandleSubmitProcedureAsync(
        Guid id,
        HttpContext ctx,
        SubmitProcedureCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, CreatePermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(
            new SubmitProcedureCommand(id, tenantId, userId), ct);

        return result.Match(
            onSuccess: dto => Results.Ok(dto),
            onFailure: MapProcedureError);
    }

    private static async Task<IResult> HandleUploadAttachmentAsync(
        Guid id,
        HttpContext ctx,
        UploadAttachmentCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, forbidden) = ExtractTenantClaims(ctx, CreatePermission);
        if (forbidden is not null) return forbidden;

        if (!ctx.Request.HasFormContentType)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "multipart/form-data requerido."));

        var form = await ctx.Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");
        var labelSlug = form["label_slug"].ToString();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "file es requerido."));
        if (string.IsNullOrWhiteSpace(labelSlug))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "label_slug es requerido."));

        await using var stream = file.OpenReadStream();
        var command = new UploadAttachmentCommand(
            ProcedureId: id,
            TenantId: tenantId,
            UserId: userId,
            LabelSlug: labelSlug,
            FileName: file.FileName,
            ContentType: file.ContentType ?? "application/octet-stream",
            SizeBytes: file.Length,
            Content: stream);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/procedures/{id}/attachments/{dto.AttachmentId}", dto),
            onFailure: MapProcedureError);
    }

    private static async Task<IResult> HandleListAttachmentsAsync(
        Guid id,
        HttpContext ctx,
        GetProcedureAttachmentsQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var procedure = await handler.HandleAsync(new GetProcedureAttachmentsQuery(id, tenantId), ct);
        return Results.Ok(procedure);
    }

    private static async Task<IResult> HandleGetSecondarySellersAsync(
        Guid id,
        HttpContext ctx,
        GetSecondarySellersQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var sellers = await handler.HandleAsync(new GetSecondarySellersQuery(id, tenantId), ct);
        return Results.Ok(sellers);
    }

    private static async Task<IResult> HandleListProceduresAsync(
        HttpContext ctx,
        ListProceduresQueryHandler handler,
        string? status,
        DateTimeOffset? fecha_from,
        int page = 1,
        int page_size = 20,
        CancellationToken ct = default)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var query = new ListProceduresQuery(tenantId, status, fecha_from, page, page_size);
        var result = await handler.HandleAsync(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetProcedureDetailAsync(
        Guid id,
        HttpContext ctx,
        GetProcedureDetailQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, forbidden) = ExtractTenantClaims(ctx, ReadPermission);
        if (forbidden is not null) return forbidden;

        var result = await handler.HandleAsync(new GetProcedureDetailQuery(id, tenantId), ct);
        return result.Match(
            onSuccess: dto => Results.Ok(dto),
            onFailure: MapProcedureError);
    }

    private static (Guid TenantId, Guid UserId, IResult? Error) ExtractTenantClaims(
        HttpContext ctx, string requiredPermission)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized));

        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token sin tenant_id."),
                statusCode: StatusCodes.Status401Unauthorized));

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        var perms = ctx.User.FindAll("perms").Select(c => c.Value).ToHashSet();

        if (!roles.Contains(SuperAdminRoleClaim) && !perms.Contains(requiredPermission))
            return (Guid.Empty, Guid.Empty, Results.Json(
                new ErrorResponse("FORBIDDEN", $"Se requiere permiso {requiredPermission}."),
                statusCode: StatusCodes.Status403Forbidden));

        return (tenantId, userId, null);
    }

    private static IResult MapProcedureError(ProcedureError err) => err.Code switch
    {
        "PROCEDURE_NOT_FOUND" or "PROCEDURE_TYPE_NOT_FOUND" or "COMPANY_NOT_FOUND" =>
            Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "PROCEDURE_INVALID_STATUS" =>
            Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "VEHICLE_QUERY_FAILED" or "PERSON_QUERY_FAILED" or "LEGAL_ENTITY_QUERY_FAILED" or
        "ACTOR_DEFINITION_NOT_FOUND" or "ACTOR_INVALID_NATURE" =>
            Results.BadRequest(new ErrorResponse(err.Code, err.Message)),
        "CUOTA_SUM_EXCEEDS_100" =>
            Results.Json(
                new CuotaValidationErrorDto(
                    err.Code,
                    err.CuotaCurrentSum ?? 0,
                    err.CuotaProposed ?? 0),
                statusCode: StatusCodes.Status422UnprocessableEntity),
        "REQUIRED_FIELDS_MISSING" =>
            Results.Json(
                new SubmitValidationErrorsDto(
                    err.ValidationErrors?
                        .Select(e => new FieldValidationErrorDto(e.FieldSlug, e.Step, e.Error))
                        .ToList() ?? []),
                statusCode: StatusCodes.Status422UnprocessableEntity),
        "FORBIDDEN" =>
            Results.Json(new ErrorResponse(err.Code, err.Message),
                statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
    };
}

public sealed record CreateProcedureRequest(
    [property: Required] Guid ProcedureTypeId,
    [property: Required] Guid CompanyId,
    Guid? OtId);

public sealed record CaptureVehicleRequest(
    string? Plate,
    string? Vin);

public sealed record AddActorRequest(
    [property: Required] Guid ActorDefinitionId,
    [property: Required] string Nature,
    string? DocumentType,
    string? DocumentNumber,
    string? Nit,
    decimal? CuotaPct);
