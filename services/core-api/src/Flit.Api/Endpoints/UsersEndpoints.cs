using Flit.Modules.Identity.Application.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints de usuarios del tenant.
/// GET /api/v1/users — listado paginado (HU-9773 frontend / diseño #9567).
/// </summary>
public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Users");

        group.MapGet("/users", HandleListUsersAsync)
            .WithName("ListUsers")
            .RequireAuthorization()
            .Produces<PaginatedUsersResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> HandleListUsersAsync(
        HttpContext ctx,
        ListUsersQueryHandler handler,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var (tenantId, _, error) = ExtractClaims(ctx);
        if (error is not null)
            return error;

        var result = await handler.HandleAsync(
            new ListUsersQuery(tenantId, page, pageSize, search), ct);

        return Results.Ok(new PaginatedUsersResponse(
            Items: result.Items.Select(u => new UserListItemResponse(
                u.Id,
                u.Email,
                u.FullName,
                u.Status,
                u.MustResetPwd,
                u.LastLoginAt,
                u.CreatedAt,
                u.Roles.Select(r => new RoleSummaryResponse(r.Id, r.Slug, r.Name)).ToArray()
            )).ToArray(),
            Total: result.Total,
            Page: result.Page,
            PageSize: result.PageSize));
    }

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
}

public sealed record UserListItemResponse(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    bool MustResetPwd,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    RoleSummaryResponse[] Roles);

public sealed record PaginatedUsersResponse(
    UserListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
