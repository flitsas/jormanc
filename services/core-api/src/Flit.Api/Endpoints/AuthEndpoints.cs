using System.ComponentModel.DataAnnotations;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;


/// <summary>
/// Endpoints de autenticación: POST /api/v1/auth/login y GET /api/v1/auth/me.
/// HU-9769 — AC1, AC2, AC3.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // AC1 + AC2: Login
        group.MapPost("/login", HandleLoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        // AC3: Perfil del usuario autenticado
        group.MapGet("/me", HandleMeAsync)
            .WithName("GetMe")
            .RequireAuthorization()
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        // HU-9772 AC3: Forgot password
        group.MapPost("/forgot-password", HandleForgotPasswordAsync)
            .WithName("ForgotPassword")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent);

        // HU-9772 AC3: Reset password
        group.MapPost("/reset-password", HandleResetPasswordAsync)
            .WithName("ResetPassword")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> HandleLoginAsync(
        [FromBody] LoginRequest request,
        LoginCommandHandler handler,
        CancellationToken ct)
    {
        if (!TryValidate(request, out var validationErrors))
            return Results.BadRequest(new ValidationErrorResponse("VALIDATION_ERROR", validationErrors));

        var command = new LoginCommand(request.Email, request.Password, request.TenantSlug);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Ok(new LoginResponse(
                AccessToken: dto.AccessToken,
                ExpiresIn: dto.ExpiresIn,
                User: new UserProfile(
                    Id: dto.User.Id,
                    Name: dto.User.Name,
                    Email: dto.User.Email,
                    Roles: dto.User.Roles,
                    Permissions: dto.User.Permissions,
                    TenantId: dto.User.TenantId,
                    TenantName: dto.User.TenantName))),
            onFailure: err => err.Code switch
            {
                "USER_NOT_ACTIVE" => Results.Json(
                    new ErrorResponse(err.Code, err.Message),
                    statusCode: StatusCodes.Status401Unauthorized),
                _ => Results.Json(
                    new ErrorResponse(err.Code, err.Message),
                    statusCode: StatusCodes.Status401Unauthorized)
            });
    }

    private static async Task<IResult> HandleMeAsync(
        HttpContext ctx,
        GetUserProfileQueryHandler handler,
        CancellationToken ct)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId) ||
            !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetUserProfileQuery(userId, tenantId), ct);

        return result.Match(
            onSuccess: dto => Results.Ok(new MeResponse(
                Id: dto.Id,
                Name: dto.Name,
                Email: dto.Email,
                Roles: dto.Roles,
                Permissions: dto.Permissions,
                TenantId: dto.TenantId,
                TenantName: dto.TenantName)),
            onFailure: err => Results.Json(
                new ErrorResponse(err.Code, err.Message),
                statusCode: StatusCodes.Status401Unauthorized));
    }

    private static async Task<IResult> HandleForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        ForgotPasswordCommandHandler handler,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.TenantSlug))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "email y tenant_slug son requeridos."));

        await handler.HandleAsync(new ForgotPasswordCommand(request.Email, request.TenantSlug), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        ResetPasswordCommandHandler handler,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "token y new_password son requeridos."));

        var result = await handler.HandleAsync(new ResetPasswordCommand(request.Token, request.NewPassword), ct);
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: err => Results.BadRequest(new ErrorResponse(err.Code, err.Message)));
    }

    private static bool TryValidate(LoginRequest request, out string[] errors)
    {
        var errorList = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Email))
            errorList.Add("email es requerido.");
        if (string.IsNullOrWhiteSpace(request.Password))
            errorList.Add("password es requerido.");
        if (string.IsNullOrWhiteSpace(request.TenantSlug))
            errorList.Add("tenant_slug es requerido.");
        errors = [.. errorList];
        return errors.Length == 0;
    }
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record LoginRequest(
    [property: Required] string Email,
    [property: Required] string Password,
    [property: Required] string TenantSlug);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserProfile User);

public sealed record MeResponse(
    Guid Id,
    string Name,
    string Email,
    string[] Roles,
    string[] Permissions,
    Guid TenantId,
    string TenantName);

public sealed record UserProfile(
    Guid Id,
    string Name,
    string Email,
    string[] Roles,
    string[] Permissions,
    Guid TenantId,
    string TenantName);

public sealed record ErrorResponse(string Code, string Message);

public sealed record ValidationErrorResponse(string Code, string[] Details);

public sealed record ForgotPasswordRequest(string Email, string TenantSlug);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
