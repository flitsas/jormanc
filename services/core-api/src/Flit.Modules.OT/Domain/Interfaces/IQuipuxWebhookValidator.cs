namespace Flit.Modules.OT.Domain.Interfaces;

/// <summary>
/// Valida firma HMAC y token del webhook Quipux contra quipux_config del OT.
/// </summary>
public interface IQuipuxWebhookValidator
{
  bool IsValid(string? quipuxConfigJson, string rawBody, string? quipuxToken, string? quipuxSignature);
}
