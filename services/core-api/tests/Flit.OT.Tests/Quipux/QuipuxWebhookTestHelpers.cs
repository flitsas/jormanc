using System.Security.Cryptography;
using System.Text;

namespace Flit.OT.Tests.Quipux;

internal static class QuipuxWebhookTestHelpers
{
  public const string DefaultToken = "test-webhook-token";

  public static string HashToken(string token)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(hash).ToLowerInvariant();
  }

  public static string ComputeSignature(string rawBody, string token)
  {
    var signature = HMACSHA256.HashData(
      Encoding.UTF8.GetBytes(token),
      Encoding.UTF8.GetBytes(rawBody));
    return Convert.ToHexString(signature).ToLowerInvariant();
  }

  public static string BuildQuipuxConfig(string token = DefaultToken) =>
    $"{{\"webhook_token_hash\":\"{HashToken(token)}\"}}";
}
