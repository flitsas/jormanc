namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Actualiza la configuración multi-pestaña de una compañía.
/// AC3 HU-9774: pestañas matrícula, traspasos, empresa y contingencia.
/// </summary>
public sealed record UpdateCompanyConfigCommand(
    Guid CompanyId,
    Guid RequestedByUserId,
    bool OnlyOwnVehicles,
    bool BaulFirmasEnabled,
    /// <summary>comprador | radicador | ninguno</summary>
    string NotificationTarget,
    /// <summary>native | api_cliente</summary>
    string SmtpMode,
    string MatriculaConfig,
    string TraspasosConfig,
    string ContingencyConfig,
    string RecaudoMethods);
