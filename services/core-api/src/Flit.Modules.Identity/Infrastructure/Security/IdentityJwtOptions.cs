namespace Flit.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Configuración JWT para el módulo Identity (emisión de tokens RS256).
/// Sección: "Jwt" en appsettings.
/// </summary>
public sealed class IdentityJwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>PEM inline de la llave privada RSA. Para producción usar PrivateKeyPath.</summary>
    public string PrivateKeyPem { get; init; } = string.Empty;

    /// <summary>Ruta al archivo .pem de llave privada RSA (relativa a ContentRootPath o absoluta).</summary>
    public string PrivateKeyPath { get; init; } = string.Empty;

    public string Issuer { get; init; } = "flit-api";
    public string Audience { get; init; } = "flit-client";

    /// <summary>Tiempo de vida del token en segundos. Default: 900 (15 min) — ADR-0013.</summary>
    public int ExpiresInSeconds { get; init; } = 900;

    /// <summary>Solo Development: genera llave RSA efímera si no hay PEM configurado.</summary>
    public bool DevGenerate { get; init; }
}
