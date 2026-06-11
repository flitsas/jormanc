using System.ComponentModel.DataAnnotations;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints RBAC: roles y permisos.
/// HU-9770 — AC1 (POST /roles), AC2 (GET /roles tenant isolation), AC3 (DELETE /roles/{id}).
/// </summary>
public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Roles");

        // AC1 — Crear rol
        group.MapPost("/roles", HandleCreateRoleAsync)
            .WithName("CreateRole")
            .RequireAuthorization()
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        // AC2 — Listar roles del tenant
        group.MapGet("/roles", HandleListRolesAsync)
            .WithName("ListRoles")
            .RequireAuthorization()
            .Produces<RoleResponse[]>(StatusCodes.Status200OK);

        // AC3 — Eliminar rol (is_system=true → 409)
        group.MapDelete("/roles/{id:guid}", HandleDeleteRoleAsync)
            .WithName("DeleteRole")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        // GET /permissions — catálogo global
        group.MapGet("/permissions", HandleListPermissionsAsync)
            .WithName("ListPermissions")
            .RequireAuthorization()
            .Produces<PermissionResponse[]>(StatusCodes.Status200OK);

        // PATCH /users/{userId}/roles — AC1
        group.MapPatch("/users/{userId:guid}/roles", HandleAssignUserRolesAsync)
            .WithName("AssignUserRoles")
            .RequireAuthorization()
            .Produces<AssignRolesResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    // ─── Handlers ────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleCreateRoleAsync(
        [FromBody] CreateRoleRequest request,
        HttpContext ctx,
        CreateRoleCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, error) = ExtractClaims(ctx);
        if (error is not null) return error;

        if (!TryValidateCreateRole(request, out var validationErrors))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", string.Join("; ", validationErrors)));

        var command = new CreateRoleCommand(
            TenantId: tenantId,
            RequestedByUserId: userId,
            Slug: request.Slug,
            Name: request.Name,
            Description: request.Description,
            PermissionIds: request.Permissions ?? []);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/roles/{dto.Id}",
                new RoleResponse(dto.Id, dto.Slug, dto.Name, dto.Description, dto.IsSystem, dto.Permissions)),
            onFailure: err => err.Code switch
            {
                "ROLE_SLUG_ALREADY_EXISTS" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
                _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
            });
    }

    private static async Task<IResult> HandleListRolesAsync(
        HttpContext ctx,
        ListRolesQueryHandler handler,
        CancellationToken ct)
    {
        var (tenantId, _, error) = ExtractClaims(ctx);
        if (error is not null) return error;

        var roles = await handler.HandleAsync(new ListRolesQuery(tenantId), ct);
        return Results.Ok(roles.Select(r =>
            new RoleResponse(r.Id, r.Slug, r.Name, r.Description, r.IsSystem, r.Permissions)).ToArray());
    }

    private static async Task<IResult> HandleDeleteRoleAsync(
        Guid id,
        HttpContext ctx,
        DeleteRoleCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, userId, error) = ExtractClaims(ctx);
        if (error is not null) return error;

        var result = await handler.HandleAsync(
            new DeleteRoleCommand(id, tenantId, userId), ct);

        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: err => err.Code switch
            {
                "ROLE_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
                "SYSTEM_ROLE_CANNOT_BE_DELETED" => Results.Conflict(new ErrorResponse(err.Code, err.Message)),
                _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
            });
    }

    private static async Task<IResult> HandleListPermissionsAsync(
        ListPermissionsQueryHandler handler,
        CancellationToken ct)
    {
        var permissions = await handler.HandleAsync(new ListPermissionsQuery(), ct);
        return Results.Ok(permissions.Select(p =>
            new PermissionResponse(p.Id, p.Slug, p.Module, p.Action, p.Description)).ToArray());
    }

    private static async Task<IResult> HandleAssignUserRolesAsync(
        Guid userId,
        [FromBody] AssignRolesRequest request,
        HttpContext ctx,
        AssignUserRolesCommandHandler handler,
        CancellationToken ct)
    {
        var (tenantId, requestedBy, error) = ExtractClaims(ctx);
        if (error is not null) return error;

        var command = new AssignUserRolesCommand(
            UserId: userId,
            TenantId: tenantId,
            RequestedByUserId: requestedBy,
            RoleIds: request.Roles ?? []);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: r => Results.Ok(new AssignRolesResponse(
                Id: r.UserId,
                Roles: r.Roles.Select(ro => new RoleSummaryResponse(ro.Id, ro.Slug, ro.Name)).ToArray())),
            onFailure: err => err.Code switch
            {
                "USER_NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Code, err.Message)),
                _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
            });
    }

    // ─── Claim helpers ───────────────────────────────────────────────────────

    private static (Guid TenantId, Guid UserId, IResult? Error) ExtractClaims(HttpContext ctx)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId) ||
            !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty,
                Results.Json(new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                    statusCode: StatusCodes.Status401Unauthorized));
        }

        return (tenantId, userId, null);
    }

    private static bool TryValidateCreateRole(CreateRoleRequest req, out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(req.Slug)) errors.Add("slug es requerido.");
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name es requerido.");
        return errors.Count == 0;
    }
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record CreateRoleRequest(
    [property: Required] string Slug,
    [property: Required] string Name,
    string? Description,
    IReadOnlyList<Guid>? Permissions);

public sealed record AssignRolesRequest(IReadOnlyList<Guid>? Roles);

public sealed record RoleResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    bool IsSystem,
    string[] Permissions);

public sealed record PermissionResponse(
    Guid Id,
    string Slug,
    string Module,
    string Action,
    string? Description);

public sealed record AssignRolesResponse(
    Guid Id,
    RoleSummaryResponse[] Roles);

public sealed record RoleSummaryResponse(Guid Id, string Slug, string Name);
