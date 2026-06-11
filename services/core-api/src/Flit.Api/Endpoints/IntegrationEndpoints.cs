using System.ComponentModel.DataAnnotations;
using Flit.Modules.Integrations.Application.Commands;
using Flit.Modules.Integrations.Application.DTOs;
using Flit.Modules.Integrations.Application.Queries;
using Flit.Modules.Integrations.Application.UseCases;
using Flit.Modules.Integrations.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints del módulo Integrations — HU-9775.
/// GET  /api/v1/admin/companies/{companyId}/config/connector  — AC1: obtener config RUNT
/// PUT  /api/v1/admin/companies/{companyId}/config/connector  — AC1: actualizar proveedor RUNT
/// GET  /api/v1/admin/integration-logs                        — AC3: listar logs por tenant
/// POST /api/v1/runt/query/plate                              — AC1+AC2+AC3: consulta vehículo
/// Solo SuperAdmin excepto /runt/query que admite roles admin_tenant.
/// </summary>
public static class IntegrationEndpoints
{
    private const string SuperAdminRoleClaim = "superadmin";

    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/v1/admin").WithTags("Integrations");
        var runtGroup = app.MapGroup("/api/v1/runt").WithTags("RUNT");

        // AC1 — Obtener configuración de conector RUNT por tenant/compañía
        adminGroup.MapGet("/companies/{companyId:guid}/config/connector",
                HandleGetConnectorConfigAsync)
            .WithName("GetConnectorConfig")
            .RequireAuthorization()
            .Produces<IReadOnlyList<ConnectorConfigDto>>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        // AC1 — Crear/actualizar proveedor RUNT por tenant/compañía
        adminGroup.MapPut("/companies/{companyId:guid}/config/connector",
                HandleUpsertConnectorConfigAsync)
            .WithName("UpsertConnectorConfig")
            .RequireAuthorization()
            .Produces<ConnectorConfigDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        // AC3 — Listar logs de payload RUNT por tenant
        adminGroup.MapGet("/integration-logs", HandleGetIntegrationLogsAsync)
            .WithName("GetIntegrationLogs")
            .RequireAuthorization()
            .Produces<IntegrationLogsPageResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        // AC1+AC2+AC3 — Consulta RUNT por placa (con failover automático)
        runtGroup.MapPost("/query/plate", HandleQueryVehicleByPlateAsync)
            .WithName("QueryVehicleByPlate")
            .RequireAuthorization()
            .Produces<VehicleQueryResultDto>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ErrorResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    // ─── Handlers ────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleGetConnectorConfigAsync(
        Guid companyId,
        HttpContext ctx,
        GetConnectorConfigQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, error) = ExtractTenantFromCompanyContext(ctx, companyId);
        if (error is not null) return error;

        var query = new GetConnectorConfigQuery(tenantId);
        var result = await handler.HandleAsync(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpsertConnectorConfigAsync(
        Guid companyId,
        [FromBody] UpsertConnectorConfigRequest request,
        HttpContext ctx,
        UpsertConnectorConfigCommandHandler handler,
        CancellationToken ct)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (!TryValidateUpsertRequest(request, out var validationErrors))
            return Results.BadRequest(
                new ErrorResponse("VALIDATION_ERROR", string.Join("; ", validationErrors)));

        // En este endpoint, companyId se mapea al tenantId del request
        // (el frontend envía el tenantId del tenant asociado a la compañía)
        var command = new UpsertConnectorConfigCommand(
            TenantId: request.TenantId,
            ConnectorType: request.ConnectorType.Trim().ToLowerInvariant(),
            Provider: request.Provider.Trim().ToLowerInvariant(),
            IsPrimary: request.IsPrimary,
            Priority: request.Priority,
            TimeoutMs: request.TimeoutMs > 0 ? request.TimeoutMs : 4000,
            IsActive: request.IsActive,
            RequestedByUserId: userId);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(dto),
            onFailure: MapIntegrationsError);
    }

    private static async Task<IResult> HandleGetIntegrationLogsAsync(
        HttpContext ctx,
        [FromQuery] Guid tenant_id,
        [FromQuery] int page,
        [FromQuery] int page_size,
        [FromQuery] string? connector_type,
        [FromQuery] string? provider,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        GetIntegrationLogsQueryHandler handler,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (tenant_id == Guid.Empty)
            return Results.BadRequest(
                new ErrorResponse("VALIDATION_ERROR", "tenant_id es requerido."));

        var query = new GetIntegrationLogsQuery(
            TenantId: tenant_id,
            Page: page < 1 ? 1 : page,
            PageSize: page_size < 1 ? 20 : page_size,
            ConnectorType: connector_type,
            Provider: provider,
            From: from,
            To: to);

        var result = await handler.HandleAsync(query, ct);

        return Results.Ok(new IntegrationLogsPageResponse(
            Data: result.Data,
            Total: result.Total,
            Page: result.Page,
            PageSize: result.PageSize));
    }

    private static async Task<IResult> HandleQueryVehicleByPlateAsync(
        [FromBody] QueryVehicleByPlateRequest request,
        HttpContext ctx,
        QueryVehicleByPlateUseCase useCase,
        CancellationToken ct)
    {
        var (_, forbiddenError) = ExtractAuthenticatedClaims(ctx);
        if (forbiddenError is not null) return forbiddenError;

        if (string.IsNullOrWhiteSpace(request.Plate))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "plate es requerido."));

        if (request.TenantId == Guid.Empty)
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "tenant_id es requerido."));

        var result = await useCase.ExecuteAsync(request.TenantId, request.Plate.Trim().ToUpperInvariant(), ct);

        return result.Match(
            onSuccess: dto => Results.Ok(dto),
            onFailure: MapIntegrationsError);
    }

    // ─── Claim helpers ───────────────────────────────────────────────────────

    private static (Guid UserId, IResult? Error) ExtractSuperAdminClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return (Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized));

        var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToHashSet();
        if (!roles.Contains(SuperAdminRoleClaim))
            return (Guid.Empty, Results.Json(
                new ErrorResponse("FORBIDDEN", "Se requiere el rol superadmin."),
                statusCode: StatusCodes.Status403Forbidden));

        return (userId, null);
    }

    private static (Guid UserId, IResult? Error) ExtractAuthenticatedClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return (Guid.Empty, Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized));

        return (userId, null);
    }

    private static (Guid TenantId, IResult? Error) ExtractTenantFromCompanyContext(
        HttpContext ctx, Guid companyId)
    {
        var (userId, forbiddenError) = ExtractSuperAdminClaims(ctx);
        if (forbiddenError is not null) return (Guid.Empty, forbiddenError);
        // El tenant_id se pasa como query param cuando el caller es SuperAdmin
        var tenantIdStr = ctx.Request.Query["tenant_id"].FirstOrDefault();
        if (!Guid.TryParse(tenantIdStr, out var tenantId))
            return (Guid.Empty, Results.BadRequest(
                new ErrorResponse("VALIDATION_ERROR", "tenant_id query param es requerido.")));
        return (tenantId, null);
    }

    // ─── Error mapper ─────────────────────────────────────────────────────────

    private static IResult MapIntegrationsError(IntegrationsError err) => err.Code switch
    {
        "INTEGRATIONS_ALL_CONNECTORS_FAILED" =>
            Results.Json(new ErrorResponse(err.Code, err.Message),
                statusCode: StatusCodes.Status503ServiceUnavailable),
        "INTEGRATIONS_CONNECTOR_CONFIG_NOT_FOUND" =>
            Results.NotFound(new ErrorResponse(err.Code, err.Message)),
        _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
    };

    private static bool TryValidateUpsertRequest(
        UpsertConnectorConfigRequest req, out List<string> errors)
    {
        errors = [];
        if (req.TenantId == Guid.Empty) errors.Add("tenant_id es requerido.");
        if (string.IsNullOrWhiteSpace(req.ConnectorType)) errors.Add("connector_type es requerido.");
        if (string.IsNullOrWhiteSpace(req.Provider)) errors.Add("provider es requerido.");
        if (req.Priority < 1) errors.Add("priority debe ser >= 1.");
        return errors.Count == 0;
    }
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record UpsertConnectorConfigRequest(
    [property: Required] Guid TenantId,
    [property: Required] string ConnectorType,
    [property: Required] string Provider,
    bool IsPrimary,
    int Priority,
    int TimeoutMs,
    bool IsActive);

public sealed record QueryVehicleByPlateRequest(
    [property: Required] Guid TenantId,
    [property: Required] string Plate);

public sealed record IntegrationLogsPageResponse(
    IReadOnlyList<IntegrationLogDto> Data,
    int Total,
    int Page,
    int PageSize);
