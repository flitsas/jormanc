using System.ComponentModel.DataAnnotations;
using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints de invitaciones: POST, GET validate, POST accept.
/// HU-9772 — AC1 y AC2.
/// </summary>
public static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/invitations").WithTags("Invitations");

        // AC1: Crear invitación
        group.MapPost("/", HandleCreateInvitationAsync)
            .WithName("CreateInvitation")
            .RequireAuthorization()
            .Produces<CreateInvitationResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        // AC2: Validar token
        group.MapGet("/{token}/validate", HandleValidateInvitationAsync)
            .WithName("ValidateInvitation")
            .AllowAnonymous()
            .Produces<ValidateInvitationResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // AC1: Aceptar invitación
        group.MapPost("/{token}/accept", HandleAcceptInvitationAsync)
            .WithName("AcceptInvitation")
            .AllowAnonymous()
            .Produces<AcceptInvitationResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> HandleCreateInvitationAsync(
        [FromBody] CreateInvitationRequest request,
        HttpContext ctx,
        CreateInvitationCommandHandler handler,
        CancellationToken ct)
    {
        var userIdStr = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var tenantIdStr = ctx.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdStr, out var invitedBy) ||
            !Guid.TryParse(tenantIdStr, out var callerTenantId))
        {
            return Results.Json(
                new ErrorResponse("UNAUTHORIZED", "Token inválido o expirado."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "email es requerido."));

        var tenantId = request.TenantId ?? callerTenantId;

        var command = new CreateInvitationCommand(
            Email: request.Email,
            RoleIds: request.RoleIds ?? [],
            TenantId: tenantId,
            InvitedBy: invitedBy);

        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Created(
                $"/api/v1/invitations/{dto.Id}",
                new CreateInvitationResponse(dto.Id, dto.Email, dto.ExpiresAt, dto.Status)),
            onFailure: err => err.Code switch
            {
                "EMAIL_ALREADY_REGISTERED" => Results.Json(
                    new ErrorResponse(err.Code, err.Message),
                    statusCode: StatusCodes.Status409Conflict),
                _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
            });
    }

    private static async Task<IResult> HandleValidateInvitationAsync(
        string token,
        ValidateInvitationQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ValidateInvitationQuery(token), ct);

        return result.Match(
            onSuccess: dto => Results.Ok(new ValidateInvitationResponse(
                dto.Email, dto.TenantId, dto.TenantName, dto.RoleSlugs)),
            onFailure: err => err.Code switch
            {
                "INVITATION_EXPIRED" => Results.BadRequest(new ErrorResponse(err.Code, err.Message)),
                "INVITATION_ALREADY_USED" => Results.BadRequest(new ErrorResponse(err.Code, err.Message)),
                _ => Results.NotFound(new ErrorResponse(err.Code, err.Message))
            });
    }

    private static async Task<IResult> HandleAcceptInvitationAsync(
        string token,
        [FromBody] AcceptInvitationRequest request,
        AcceptInvitationCommandHandler handler,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new ErrorResponse("VALIDATION_ERROR", "full_name y password son requeridos."));

        var command = new AcceptInvitationCommand(token, request.FullName, request.Password);
        var result = await handler.HandleAsync(command, ct);

        return result.Match(
            onSuccess: dto => Results.Json(
                new AcceptInvitationResponse(
                    AccessToken: dto.AccessToken,
                    ExpiresIn: dto.ExpiresIn,
                    User: new InvitationUserProfile(
                        dto.User.Id, dto.User.Name, dto.User.Email,
                        dto.User.Roles, dto.User.Permissions)),
                statusCode: StatusCodes.Status201Created),
            onFailure: err => Results.BadRequest(new ErrorResponse(err.Code, err.Message)));
    }
}

// ─── Request / Response DTOs ──────────────────────────────────────────────────

public sealed record CreateInvitationRequest(
    [property: Required] string Email,
    Guid[]? RoleIds,
    Guid? TenantId);

public sealed record CreateInvitationResponse(
    Guid Id,
    string Email,
    DateTimeOffset ExpiresAt,
    string Status);

public sealed record ValidateInvitationResponse(
    string Email,
    Guid TenantId,
    string TenantName,
    string[] RoleSlugs);

public sealed record AcceptInvitationRequest(
    [property: Required] string FullName,
    [property: Required] string Password);

public sealed record AcceptInvitationResponse(
    string AccessToken,
    int ExpiresIn,
    InvitationUserProfile User);

public sealed record InvitationUserProfile(
    Guid Id,
    string Name,
    string Email,
    string[] Roles,
    string[] Permissions);
