namespace Flit.Modules.OT.Application.Commands;

/// <summary>
/// Webhook Quipux — hot-update de estado de trámite (HU-9800).
/// </summary>
public sealed record ProcessQuipuxWebhookCommand(
  string OtSlug,
  string RawBody,
  string? QuipuxToken,
  string? QuipuxSignature);
