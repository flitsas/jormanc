using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Singleton que gestiona el par de claves RSA para firmar y validar JWTs.
/// Se construye manualmente en Program.cs para poder pasarlo tanto a JwtTokenIssuer
/// como a la configuración de JwtBearer sin problemas de orden de DI.
/// </summary>
public sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public RsaSecurityKey SigningKey { get; }

    public RsaKeyProvider(IdentityJwtOptions options, string? contentRootPath = null)
    {
        _rsa = CreateRsa(options, contentRootPath);
        SigningKey = new RsaSecurityKey(_rsa);
    }

    private static RSA CreateRsa(IdentityJwtOptions options, string? contentRootPath)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(options.PrivateKeyPem.AsSpan());
            return rsa;
        }

        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPath))
        {
            var path = Path.IsPathRooted(options.PrivateKeyPath)
                ? options.PrivateKeyPath
                : Path.Combine(contentRootPath ?? Directory.GetCurrentDirectory(), options.PrivateKeyPath);

            if (File.Exists(path))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(path).AsSpan());
                return rsa;
            }
        }

        // Busca en la ubicación canónica del repositorio (infra/secrets/)
        if (contentRootPath is not null)
        {
            var repoKey = Path.GetFullPath(
                Path.Combine(contentRootPath, "../../../../infra/secrets/jwt-private.pem"));
            if (File.Exists(repoKey))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(repoKey).AsSpan());
                return rsa;
            }
        }

        if (options.DevGenerate)
        {
            // Llave efímera para desarrollo: válida solo durante la vida del proceso
            return RSA.Create(2048);
        }

        throw new InvalidOperationException(
            "JWT private key no configurada. " +
            "Configura Jwt:PrivateKeyPem, Jwt:PrivateKeyPath o usa Jwt:DevGenerate=true (solo Development).");
    }

    public void Dispose() => _rsa.Dispose();
}
