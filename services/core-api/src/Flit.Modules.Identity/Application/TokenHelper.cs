using System.Security.Cryptography;
using System.Text;

namespace Flit.Modules.Identity.Application;

/// <summary>
/// Genera tokens URL-safe y calcula su SHA-256 para almacenamiento seguro.
/// El token en texto plano va al email; el hash va a la BD.
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// Genera un token URL-safe de 32 bytes aleatorios (base64url, sin padding).
    /// Suficientemente entrópico para tokens de invitación y reset de contraseña.
    /// </summary>
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Calcula el hash SHA-256 (hex lowercase) de un token raw.
    /// Este valor es el que se almacena en token_hash de BD.
    /// </summary>
    public static string Hash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
