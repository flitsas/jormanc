using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Errors;

namespace Flit.Api.Endpoints;

/// <summary>
/// Webhook Quipux — POST /api/v1/ot/webhooks/quipux/{ot_slug} (HU-9800).
/// </summary>
public static class OtWebhooksEndpoints
{
  public const string QuipuxTokenHeader = "X-Quipux-Token";
  public const string QuipuxSignatureHeader = "X-Quipux-Signature";

  public static IEndpointRouteBuilder MapOtWebhooksEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/v1/ot/webhooks").WithTags("OT Webhooks");

    group.MapPost("/quipux/{ot_slug}", HandleQuipuxWebhookAsync)
      .WithName("QuipuxWebhook")
      .AllowAnonymous()
      .Produces<QuipuxWebhookApiResponse>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .Produces(StatusCodes.Status401Unauthorized)
      .Produces(StatusCodes.Status404NotFound);

    return app;
  }

  private static async Task<IResult> HandleQuipuxWebhookAsync(
    string ot_slug,
    HttpContext ctx,
    ProcessQuipuxWebhookCommandHandler handler,
    CancellationToken ct)
  {
    ctx.Request.EnableBuffering();
    using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
    var rawBody = await reader.ReadToEndAsync(ct);
    ctx.Request.Body.Position = 0;

    var token = ctx.Request.Headers[QuipuxTokenHeader].FirstOrDefault();
    var signature = ctx.Request.Headers[QuipuxSignatureHeader].FirstOrDefault();

    var command = new ProcessQuipuxWebhookCommand(ot_slug, rawBody, token, signature);
    var result = await handler.HandleAsync(command, ct);

    return result.Match(
      onSuccess: dto => Results.Ok(new QuipuxWebhookApiResponse(
        dto.EventType,
        dto.ProcedureRef,
        dto.NewStatus,
        dto.Processed)),
      onFailure: MapWebhookError);
  }

  private static IResult MapWebhookError(OtError err) => err.Code switch
  {
    "OT_INVALID_WEBHOOK_SIGNATURE" => Results.Json(
      new ErrorResponse("UNAUTHORIZED", "No autorizado."),
      statusCode: StatusCodes.Status401Unauthorized),
    "OT_SLUG_NOT_FOUND" or "OT_PROCEDURE_NOT_FOUND" => Results.NotFound(
      new ErrorResponse(err.Code, err.Message)),
    _ => Results.BadRequest(new ErrorResponse(err.Code, err.Message))
  };
}

public sealed record QuipuxWebhookApiResponse(
  string EventType,
  string? ProcedureRef,
  string? NewStatus,
  bool Processed);
