using System.ComponentModel.DataAnnotations;
using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Application.Queries;
using Flit.Modules.Companies.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints de administración de compañías — solo SuperAdmin.
/// HU-9774 — AC1 (POST /companies), AC2 (GET /companies/index), AC3 (PATCH /companies/{id}/config).
/// HU-9776 — AC1 (PUT /companies/{id}/config/signature-matrix),
///            AC2 (GET|POST|DELETE /companies/{id}/user-exceptions),
///            AC3 (GET|PUT /companies/{id}/config/ot-enabled).
/// </summary>
public static class CompaniesEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";

    public static IEndpointRouteBuilder MapCompaniesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/companies").WithTags("Companies");

        // AC1 — Crear compañía + tenant + config por defecto
        group.MapPost("/", HandleCreateCompanyAsync)
            .WithName("CreateCompany")
            .RequireAuthorization()
            .Produces<CompanyResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        // AC2 — Listar compañías con paginación y filtros
        group.MapGet("/index", HandleListCompaniesAsync)
            .WithName("ListCompanies")
            .RequireAuthorization()
            .Produces<CompanyPageResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        // AC3 — Actualizar configuración multi-pestaña
        group.MapPatch("/{id:guid}/config", HandleUpdateCompanyConfigAsync)
            .WithName("UpdateCompanyConfig")
            .RequireAuthorization()
            .Produces<CompanyConfigResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ── HU-9776 — Signature Matrix (AC1) ──────────────────────────────────
        group.MapGet("/{id:guid}/config/signature-matrix", HandleGetSignatureMatrixAsync)
            .WithName("GetSignatureMatrix")
            .RequireAuthorization()
            .Produces<SignatureMatrixResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/config/signature-matrix", HandleUpdateSignatureMatrixAsync)
            .WithName("UpdateSignatureMatrix")
            .RequireAuthorization()
            .Produces<SignatureMatrixResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ── HU-9776 — User Exceptions (AC2) ───────────────────────────────────
        group.MapGet("/{id:guid}/user-exceptions", HandleGetUserExceptionsAsync)
            .WithName("GetUserExceptions")
            .RequireAuthorization()
            .Produces<UserExceptionResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/user-exceptions", HandleAddUserExceptionAsync)
            .WithName("AddUserException")
            .RequireAuthorization()
            .Produces<UserExceptionResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/user-exceptions/{userId:guid}", HandleRemoveUserExceptionAsync)
            .WithName("RemoveUserException")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ── HU-9776 — OT Enabled (AC3) ────────────────────────────────────────
        group.MapGet("/{id:guid}/config/ot-enabled", HandleGetOtEnabledAsync)
            .WithName("GetOtEnabled")
            .RequireAuthorization()
            .Produces<OtEnabledResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/config/ot-enabled", HandleUpdateOtEnabledAsync)
            .WithName("UpdateOtEnabled")
            .RequireAuthorization()
            .Produces<OtEnabledResponse[]>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    // ─── Handlers ────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleCreateCompanyAsync(
        [FromBody] CreateCompanyRequest request,
        HttpContext ctx,
        CreateCompanyCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (!TryValidateCreateCompany(request, out var errors))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", string.Join("; ", errors)));

        var command = new CreateCompanyCommand(
            Nit: request.Nit.Trim(),
            Name: request.Name.Trim(),
            TenantSlug: request.TenantSlug.Trim().ToLowerInvariant(),
            RequestedByUserId: userId);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/admin/companies/{dto.Id}",
                new CompanyResponse(
                    dto.Id, dto.TenantId, dto.TenantSlug,
                    dto.Nit, dto.Name, dto.Status, dto.CreatedAt)),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleListCompaniesAsync(
        HttpContext ctx,
        [FromQuery] int page,
        [FromQuery] int page_size,
        [FromQuery] string? nit,
        [FromQuery] string? name,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? created_from,
        [FromQuery] DateTimeOffset? created_to,
        ListCompaniesQueryHandler handler,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        var effectivePage = page < 1 ? 1 : page;
        var effectivePageSize = page_size < 1 ? 20 : page_size;

        var query = new ListCompaniesQuery(
            Page: effectivePage,
            PageSize: effectivePageSize,
            Nit: nit,
            Name: name,
            Status: status,
            CreatedFrom: created_from,
            CreatedTo: created_to);

        var result = await handler.HandleAsync(query, ct);

        return Results.Ok(new CompanyPageResponse(
            Data: result.Data.Select(d =>
                new CompanyListItemResponse(d.Id, d.TenantId, d.Nit, d.Name, d.Status, d.TenantSlug, d.CreatedAt)).ToArray(),
            Total: result.Total,
            Page: result.Page,
            PageSize: result.PageSize));
    }

    private static async Task<IResult> HandleUpdateCompanyConfigAsync(
        Guid id,
        [FromBody] UpdateCompanyConfigRequest request,
        HttpContext ctx,
        UpdateCompanyConfigCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (!TryValidateUpdateConfig(request, out var errors))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", string.Join("; ", errors)));

        var command = new UpdateCompanyConfigCommand(
            CompanyId: id,
            RequestedByUserId: userId,
            OnlyOwnVehicles: request.OnlyOwnVehicles,
            BaulFirmasEnabled: request.BaulFirmasEnabled,
            NotificationTarget: request.NotificationTarget,
            SmtpMode: request.SmtpMode,
            MatriculaConfig: request.MatriculaConfig ?? "{}",
            TraspasosConfig: request.TraspasosConfig ?? "{}",
            ContingencyConfig: request.ContingencyConfig ?? "{}",
            RecaudoMethods: request.RecaudoMethods ?? "[]");

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: cfg => Results.Ok(new CompanyConfigResponse(
                cfg.Id, cfg.CompanyId, cfg.OnlyOwnVehicles, cfg.BaulFirmasEnabled,
                cfg.NotificationTarget, cfg.SmtpMode,
                cfg.MatriculaConfig, cfg.TraspasosConfig,
                cfg.ContingencyConfig, cfg.RecaudoMethods)),
            onFailure: MapCompanyError);
    }

    // ── HU-9776 Handlers ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleGetSignatureMatrixAsync(
        Guid id,
        HttpContext ctx,
        GetSignatureMatrixQueryHandler handler,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        var result = await handler.HandleAsync(new GetSignatureMatrixQuery(id), ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapSignatureMatrix).ToArray()),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleUpdateSignatureMatrixAsync(
        Guid id,
        [FromBody] UpdateSignatureMatrixRequest request,
        HttpContext ctx,
        UpdateSignatureMatrixCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (request.Entries is null || request.Entries.Length == 0)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "entries no puede estar vacío."));

        var entries = request.Entries
            .Select(e => new SignatureMatrixEntry(
                ActorRole: e.ActorRole?.Trim() ?? string.Empty,
                SignatureType: e.SignatureType?.Trim() ?? string.Empty,
                IsActive: e.IsActive))
            .ToList();

        var command = new UpdateSignatureMatrixCommand(id, userId, entries);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapSignatureMatrix).ToArray()),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleGetUserExceptionsAsync(
        Guid id,
        HttpContext ctx,
        GetUserExceptionsQueryHandler handler,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        var result = await handler.HandleAsync(new GetUserExceptionsQuery(id), ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapUserException).ToArray()),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleAddUserExceptionAsync(
        Guid id,
        [FromBody] AddUserExceptionRequest request,
        HttpContext ctx,
        AddUserExceptionCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (request.UserId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "user_id es requerido."));

        var command = new AddUserExceptionCommand(
            CompanyId: id,
            UserId: request.UserId,
            AddedByUserId: userId);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/admin/companies/{id}/user-exceptions",
                MapUserException(dto)),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleRemoveUserExceptionAsync(
        Guid id,
        Guid userId,
        HttpContext ctx,
        RemoveUserExceptionCommandHandler handler,
        CancellationToken ct)
    {
        var (requesterId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        var command = new RemoveUserExceptionCommand(
            CompanyId: id,
            UserId: userId,
            RequestedByUserId: requesterId);

        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleGetOtEnabledAsync(
        Guid id,
        HttpContext ctx,
        GetOtEnabledQueryHandler handler,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        var result = await handler.HandleAsync(new GetOtEnabledQuery(id), ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapOtEnabled).ToArray()),
            onFailure: MapCompanyError);
    }

    private static async Task<IResult> HandleUpdateOtEnabledAsync(
        Guid id,
        [FromBody] UpdateOtEnabledRequest request,
        HttpContext ctx,
        UpdateOtEnabledCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (request.Entries is null)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "entries es requerido."));

        var entries = request.Entries
            .Select(e => new OtEnabledEntry(
                OtSlug: e.OtSlug?.Trim() ?? string.Empty,
                ProcedureFamily: e.ProcedureFamily?.Trim() ?? string.Empty,
                IsEnabled: e.IsEnabled))
            .ToList();

        var command = new UpdateOtEnabledCommand(id, userId, entries);
        var result = await handler.HandleAsync(command, ct);
        return result.Match(
            onSuccess: dtos => Results.Ok(dtos.Select(MapOtEnabled).ToArray()),
            onFailure: MapCompanyError);
    }

    // ─── Mappers ─────────────────────────────────────────────────────────────

    private static SignatureMatrixResponse MapSignatureMatrix(SignatureMatrixEntryDto dto) =>
        new(dto.Id, dto.CompanyId, dto.ActorRole, dto.SignatureType, dto.IsActive);

    private static UserExceptionResponse MapUserException(UserExceptionDto dto) =>
        new(dto.Id, dto.CompanyId, dto.UserId, dto.AddedBy, dto.AddedAt);

    private static OtEnabledResponse MapOtEnabled(OtEnabledEntryDto dto) =>
        new(dto.Id, dto.CompanyId, dto.OtSlug, dto.ProcedureFamily, dto.IsEnabled);

    // ─── Claim helpers ───────────────────────────────────────────────────────

    private static (Guid UserId, IResult? Error) ExtractSuperAdminClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return (Guid.Empty,
                Results.Json(new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                    statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        if (!roles.Contains(SuperAdminRoleClaim))
        {
            return (Guid.Empty,
                Results.Json(new ErrorResponse("FORBIDDEN", "Se requiere el rol superadmin."),
                    statusCode: StatusCodes.Status403Forbidden));
        }

        return (userId, null);
    }

    private static IResult MapCompanyError(CompanyError err) => err.Code switch
    {
        "COMPANY_NIT_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "COMPANY_TENANT_SLUG_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "COMPANY_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "COMPANY_CONFIG_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        "COMPANY_USER_EXCEPTION_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
        "COMPANY_USER_EXCEPTION_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
    };

    private static bool TryValidateCreateCompany(CreateCompanyRequest req, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(req.Nit)) errors.Add("nit es requerido.");
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name es requerido.");
        if (string.IsNullOrWhiteSpace(req.TenantSlug)) errors.Add("tenant_slug es requerido.");
        return errors.Count == 0;
    }

    private static bool TryValidateUpdateConfig(UpdateCompanyConfigRequest req, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(req.NotificationTarget)) errors.Add("notification_target es requerido.");
        if (string.IsNullOrWhiteSpace(req.SmtpMode)) errors.Add("smtp_mode es requerido.");
        return errors.Count == 0;
    }
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record CreateCompanyRequest(
    [property: Required] string Nit,
    [property: Required] string Name,
    [property: Required] string TenantSlug);

public sealed record UpdateCompanyConfigRequest(
    bool OnlyOwnVehicles,
    bool BaulFirmasEnabled,
    [property: Required] string NotificationTarget,
    [property: Required] string SmtpMode,
    string? MatriculaConfig,
    string? TraspasosConfig,
    string? ContingencyConfig,
    string? RecaudoMethods);

public sealed record CompanyResponse(
    Guid Id,
    Guid TenantId,
    string TenantSlug,
    string Nit,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record CompanyListItemResponse(
    Guid Id,
    Guid TenantId,
    string Nit,
    string Name,
    string Status,
    string TenantSlug,
    DateTimeOffset CreatedAt);

public sealed record CompanyPageResponse(
    CompanyListItemResponse[] Data,
    int Total,
    int Page,
    int PageSize);

public sealed record CompanyConfigResponse(
    Guid Id,
    Guid CompanyId,
    bool OnlyOwnVehicles,
    bool BaulFirmasEnabled,
    string NotificationTarget,
    string SmtpMode,
    string MatriculaConfig,
    string TraspasosConfig,
    string ContingencyConfig,
    string RecaudoMethods);

// ── HU-9776 — Signature Matrix ────────────────────────────────────────────────

public sealed record SignatureMatrixEntryRequest(
    [property: Required] string ActorRole,
    [property: Required] string SignatureType,
    bool IsActive = true);

public sealed record UpdateSignatureMatrixRequest(
    [property: Required] SignatureMatrixEntryRequest[] Entries);

public sealed record SignatureMatrixResponse(
    Guid Id,
    Guid CompanyId,
    string ActorRole,
    string SignatureType,
    bool IsActive);

// ── HU-9776 — User Exceptions ─────────────────────────────────────────────────

public sealed record AddUserExceptionRequest(
    [property: Required] Guid UserId);

public sealed record UserExceptionResponse(
    Guid Id,
    Guid CompanyId,
    Guid UserId,
    Guid? AddedBy,
    DateTimeOffset AddedAt);

// ── HU-9776 — OT Enabled ──────────────────────────────────────────────────────

public sealed record OtEnabledEntryRequest(
    [property: Required] string OtSlug,
    [property: Required] string ProcedureFamily,
    bool IsEnabled = true);

public sealed record UpdateOtEnabledRequest(
    [property: Required] OtEnabledEntryRequest[] Entries);

public sealed record OtEnabledResponse(
    Guid Id,
    Guid CompanyId,
    string OtSlug,
    string ProcedureFamily,
    bool IsEnabled);
