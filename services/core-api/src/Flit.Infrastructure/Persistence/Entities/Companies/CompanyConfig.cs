namespace Flit.Infrastructure.Persistence.Entities.Companies;

/// <summary>
/// Configuración multi-pestaña de una compañía (1:1 con Company).
/// schema: companies / tabla: company_configs
/// </summary>
public sealed class CompanyConfig
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public bool OnlyOwnVehicles { get; set; }
    public bool BaulFirmasEnabled { get; set; }
    /// <summary>comprador | radicador | ninguno</summary>
    public string NotificationTarget { get; set; } = "radicador";
    /// <summary>native | api_cliente</summary>
    public string SmtpMode { get; set; } = "native";
    /// <summary>Configuración SMTP encriptada con clave derivada del tenant_id (AES-256).</summary>
    public byte[]? SmtpConfigEncrypted { get; set; }
    /// <summary>JSONB — configuración específica de matrícula inicial.</summary>
    public string MatriculaConfig { get; set; } = "{}";
    /// <summary>JSONB — configuración específica de traspasos.</summary>
    public string TraspasosConfig { get; set; } = "{}";
    /// <summary>JSONB — configuración de contingencia FLIT.</summary>
    public string ContingencyConfig { get; set; } = "{}";
    /// <summary>JSONB — lista de métodos de recaudo habilitados.</summary>
    public string RecaudoMethods { get; set; } = "[]";
    public DateTimeOffset UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
}
