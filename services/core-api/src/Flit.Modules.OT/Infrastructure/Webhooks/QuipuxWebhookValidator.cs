using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flit.Modules.OT.Domain.Interfaces;

namespace Flit.Modules.OT.Infrastructure.Webhooks;

/// <summary>
/// Valida token (SHA-256 vs webhook_token_hash) y firma HMAC-SHA256 del body.
/// </summary>
public sealed class QuipuxWebhookValidator : IQuipuxWebhookValidator
{
  public bool IsValid(
    string? quipuxConfigJson,
    string rawBody,
    string? quipuxToken,
    string? quipuxSignature)
  {
    if (string.IsNullOrWhiteSpace(quipuxConfigJson)
        || string.IsNullOrWhiteSpace(quipuxToken)
        || string.IsNullOrWhiteSpace(quipuxSignature))
      return false;

    try
    {
      using var doc = JsonDocument.Parse(quipuxConfigJson);
      if (!doc.RootElement.TryGetProperty("webhook_token_hash", out var hashProp))
        return false;

      var storedHash = hashProp.GetString();
      if (string.IsNullOrWhiteSpace(storedHash))
        return false;

      var tokenHash = HashToken(quipuxToken.Trim());
      if (!string.Equals(tokenHash, storedHash.Trim(), StringComparison.OrdinalIgnoreCase))
        return false;

      var keyBytes = Encoding.UTF8.GetBytes(quipuxToken.Trim());
      var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
      var computed = HMACSHA256.HashData(keyBytes, bodyBytes);
      var computedHex = Convert.ToHexString(computed).ToLowerInvariant();
      var provided = quipuxSignature.Trim().ToLowerInvariant();

      return computedHex == provided;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static string HashToken(string rawToken)
  {
    var bytes = Encoding.UTF8.GetBytes(rawToken);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }
}
