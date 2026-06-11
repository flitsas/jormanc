using Flit.Modules.Analytics.Application.Commands;
using Flit.Modules.Analytics.Application.DTOs;
using Flit.Modules.Analytics.Application.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints del dashboard analítico — HU-9794 / HU-9795.
/// GET /api/v1/dashboard/summary
/// GET /api/v1/dashboard/procedures
/// GET /api/v1/dashboard/export/excel
/// GET /api/v1/dashboard/export/pdf
/// GET /api/v1/dashboard/top-users
/// </summary>
public static class DashboardEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";
    private const string TenantAdminRoleClaim = "tenantadmin";
    private const string ReadPermission = "analytics.read";
    private const string ExportPermission = "analytics.export";

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard").WithTags("Dashboard");

        group.MapGet("/summary", HandleGetSummaryAsync)
            .WithName("GetDashboardSummary")
            .RequireAuthorization()
            .Produces<DashboardSummaryDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/procedures", HandleGetProceduresAsync)
            .WithName("GetDashboardProcedures")
            .RequireAuthorization()
            .Produces<DashboardProceduresPageDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/export/excel", HandleExportExcelAsync)
            .WithName("ExportDashboardExcel")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/export/pdf", HandleExportPdfAsync)
            .WithName("ExportDashboardPdf")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/top-users", HandleGetTopUsersAsync)
            .WithName("GetDashboardTopUsers")
            .RequireAuthorization()
            .Produces<DashboardTopUsersDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> HandleGetSummaryAsync(
        HttpContext ctx,
        GetDashboardSummaryQueryHandler handler,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? tenant_id,
        CancellationToken ct)
    {
        var (tenantId, forbidden) = ResolveTenantAccess(ctx, tenant_id);
        if (forbidden is not null) return forbidden;

        var validation = ValidateDateRange(from, to);
        if (validation is not null) return validation;

        var result = await handler.HandleAsync(
            new GetDashboardSummaryQuery(tenantId, from!.Value, to!.Value), ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetProceduresAsync(
        HttpContext ctx,
        GetDashboardProceduresQueryHandler handler,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? family,
        string? status,
        [FromQuery(Name = "user_ids")] Guid[]? user_ids,
        Guid? tenant_id,
        int page = 1,
        int page_size = 20,
        CancellationToken ct = default)
    {
        var (tenantId, forbidden) = ResolveTenantAccess(ctx, tenant_id);
        if (forbidden is not null) return forbidden;

        var validation = ValidateDateRange(from, to);
        if (validation is not null) return validation;

        if (!string.IsNullOrWhiteSpace(family)
            && family is not ("matricula_inicial" or "traspasos" or "otros"))
        {
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR",
                "family debe ser matricula_inicial, traspasos u otros."));
        }

        IReadOnlyList<Guid>? userIds = user_ids is { Length: > 0 }
            ? user_ids
            : null;

        var result = await handler.HandleAsync(
            new GetDashboardProceduresQuery(
                TenantId: tenantId,
                From: from!.Value,
                To: to!.Value,
                Family: family,
                Status: status,
                UserIds: userIds,
                Page: page,
                PageSize: page_size),
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleExportExcelAsync(
        HttpContext ctx,
        ExportDashboardExcelCommandHandler handler,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? family,
        string? status,
        [FromQuery(Name = "user_ids")] Guid[]? user_ids,
        Guid? tenant_id,
        string? format,
        CancellationToken ct)
    {
        var (tenantId, forbidden) = ResolveExportAccess(ctx, tenant_id);
        if (forbidden is not null) return forbidden;

        var validation = ValidateDateRange(from, to);
        if (validation is not null) return validation;

        if (!string.IsNullOrWhiteSpace(family)
            && family is not ("matricula_inicial" or "traspasos" or "otros"))
        {
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR",
                "family debe ser matricula_inicial, traspasos u otros."));
        }

        var effectiveFormat = string.IsNullOrWhiteSpace(format) ? "xlsx" : format;
        if (effectiveFormat is not ("xlsx" or "csv"))
        {
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR",
                "format debe ser xlsx o csv."));
        }

        IReadOnlyList<Guid>? userIds = user_ids is { Length: > 0 } ? user_ids : null;

        var result = await handler.HandleAsync(
            new ExportDashboardExcelCommand(
                TenantId: tenantId,
                From: from!.Value,
                To: to!.Value,
                Family: family,
                Status: status,
                UserIds: userIds,
                Format: effectiveFormat),
            ct);

        return Results.File(result.Content, result.ContentType, result.Filename);
    }

    private static async Task<IResult> HandleGetTopUsersAsync(
        HttpContext ctx,
        GetDashboardTopUsersQueryHandler handler,
        DateTimeOffset? from,
        DateTimeOffset? to,
        [FromQuery(Name = "user_ids")] Guid[]? user_ids,
        Guid? tenant_id,
        CancellationToken ct)
    {
        var (tenantId, forbidden) = ResolveTenantAccess(ctx, tenant_id);
        if (forbidden is not null) return forbidden;

        var validation = ValidateDateRange(from, to);
        if (validation is not null) return validation;

        IReadOnlyList<Guid>? userIds = user_ids is { Length: > 0 } ? user_ids : null;

        var result = await handler.HandleAsync(
            new GetDashboardTopUsersQuery(tenantId, from!.Value, to!.Value, userIds),
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleExportPdfAsync(
        HttpContext ctx,
        ExportDashboardPdfCommandHandler handler,
        DateTimeOffset? from,
        DateTimeOffset? to,
        [FromQuery(Name = "families")] string[]? families,
        bool include_charts = true,
        Guid? tenant_id = null,
        CancellationToken ct = default)
    {
        var (tenantId, forbidden) = ResolveExportAccess(ctx, tenant_id);
        if (forbidden is not null) return forbidden;

        var validation = ValidateDateRange(from, to);
        if (validation is not null) return validation;

        if (families is { Length: > 0 })
        {
            var invalid = families.FirstOrDefault(f =>
                f is not ("matricula_inicial" or "traspasos" or "otros"));
            if (invalid is not null)
            {
                return Results.BadRequest(new ErrorResponse(
                    "VALIDATION_ERROR",
                    "families solo admite matricula_inicial, traspasos u otros."));
            }
        }

        IReadOnlyList<string>? familyFilter = families is { Length: > 0 } ? families : null;

        var result = await handler.HandleAsync(
            new ExportDashboardPdfCommand(
                TenantId: tenantId,
                From: from!.Value,
                To: to!.Value,
                Families: familyFilter,
                IncludeCharts: include_charts),
            ct);

        return Results.File(
            result.Content,
            result.ContentType,
            result.Filename);
    }

    private static IResult? ValidateDateRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is null || to is null)
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR", "Los parámetros from y to son requeridos (ISO 8601)."));

        if (from > to)
            return Results.BadRequest(new ErrorResponse(
                "VALIDATION_ERROR", "from no puede ser posterior a to."));

        return null;
    }

    private static (Guid TenantId, IResult? Error) ResolveExportAccess(
        HttpContext ctx,
        Guid? requestedTenantId)
    {
        var (tenantId, error) = ResolveTenantAccess(ctx, requestedTenantId, ExportPermission);
        return (tenantId, error);
    }

    private static (Guid TenantId, IResult? Error) ResolveTenantAccess(
        HttpContext ctx,
        Guid? requestedTenantId,
        string requiredPermission = ReadPermission)
    {
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tenantIdStr, out var tokenTenantId))
        {
            return (Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token sin tenant_id."),
                statusCode: StatusCodes.Status401Unauthorized));
        }

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var perms = ctx.User.FindAll("perms").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isSuperAdmin = roles.Contains(SuperAdminRoleClaim);
        var isTenantAdmin = roles.Contains(TenantAdminRoleClaim);
        var hasPermission = perms.Contains(requiredPermission);

        if (!isSuperAdmin && !isTenantAdmin && !hasPermission)
        {
            return (Guid.Empty, Results.Json(
                new ErrorResponse(
                    "FORBIDDEN",
                    $"Se requiere rol superadmin/tenantadmin o permiso {requiredPermission}."),
                statusCode: StatusCodes.Status403Forbidden));
        }

        if (requestedTenantId.HasValue)
        {
            if (!isSuperAdmin)
            {
                return (Guid.Empty, Results.Json(
                    new ErrorResponse(
                        "FORBIDDEN",
                        "Solo superadmin puede especificar tenant_id."),
                    statusCode: StatusCodes.Status403Forbidden));
            }

            return (requestedTenantId.Value, null);
        }

        return (tokenTenantId, null);
    }
}
