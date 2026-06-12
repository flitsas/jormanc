using Flit.Modules.Identity.Application;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.Modules.Identity.Infrastructure.Email;
using Flit.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace Flit.Api.Endpoints;

/// <summary>
/// Utilidades solo DEV/QA: bandeja de correos en memoria y expirar invitaciones en E2E.
/// </summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dev").WithTags("Dev");

        group.MapGet("/sent-emails", HandleGetSentEmailsAsync)
            .WithName("DevGetSentEmails")
            .AllowAnonymous()
            .Produces<DevSentEmailResponse[]>(StatusCodes.Status200OK);

        group.MapPost("/invitations/{token}/expire", HandleExpireInvitationAsync)
            .WithName("DevExpireInvitation")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult HandleGetSentEmailsAsync(
        [FromQuery] string? to,
        [FromServices] ConsoleEmailSender emailSender)
    {
        var emails = emailSender.SentEmails.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(to))
            emails = emails.Where(e => e.To.Equals(to, StringComparison.OrdinalIgnoreCase));

        var result = emails
            .OrderByDescending(e => e.SentAt)
            .Select(e => new DevSentEmailResponse(e.To, e.Subject, e.HtmlBody, e.SentAt))
            .ToArray();

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleExpireInvitationAsync(
        string token,
        [FromServices] IInvitationRepository invitationRepository,
        [FromServices] IClock clock,
        CancellationToken ct)
    {
        var tokenHash = TokenHelper.Hash(token);
        var invitation = await invitationRepository.FindByTokenHashAsync(tokenHash, ct);
        if (invitation is null)
            return Results.NotFound(new ErrorResponse("INVITATION_NOT_FOUND", "Invitación no encontrada."));

        invitation.ExpiresAt = clock.UtcNow.AddHours(-1);
        await invitationRepository.UpdateAsync(invitation, ct);
        return Results.NoContent();
    }
}

public sealed record DevSentEmailResponse(
    string To,
    string Subject,
    string HtmlBody,
    DateTimeOffset SentAt);
