namespace Flit.Modules.Companies.Application.DTOs;

/// <summary>
/// DTO de compañía completo (detalle y respuesta de creación).
/// </summary>
public sealed record CompanyDto(
    Guid Id,
    Guid TenantId,
    string TenantSlug,
    string Nit,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>
/// DTO liviano para la lista de compañías (AC2 — grid paginado).
/// </summary>
public sealed record CompanyListItemDto(
    Guid Id,
    Guid TenantId,
    string Nit,
    string Name,
    string Status,
    string TenantSlug,
    DateTimeOffset CreatedAt);

/// <summary>
/// Respuesta paginada de compañías.
/// </summary>
public sealed record CompanyPageDto(
    IReadOnlyList<CompanyListItemDto> Data,
    int Total,
    int Page,
    int PageSize);

/// <summary>
/// DTO de configuración multi-pestaña (AC3).
/// </summary>
public sealed record CompanyConfigDto(
    Guid Id,
    Guid CompanyId,
    bool OnlyOwnVehicles,
    bool BaulFirmasEnabled,
    string NotificationTarget,
    string SmtpMode,
    string MatriculaConfig,
    string TraspasosConfig,
    string ContingencyConfig,
    string RecaudoMethods);

/// <summary>
/// DTO de una entrada de la matriz de firmas (AC1 — HU-9776).
/// </summary>
public sealed record SignatureMatrixEntryDto(
    Guid Id,
    Guid CompanyId,
    /// <summary>vendedor | comprador | representante_legal</summary>
    string ActorRole,
    /// <summary>identidad_digital | firma_pantalla | preasignada</summary>
    string SignatureType,
    bool IsActive);

/// <summary>
/// DTO de excepción de usuario (bypass only_own_vehicles) (AC2 — HU-9776).
/// </summary>
public sealed record UserExceptionDto(
    Guid Id,
    Guid CompanyId,
    Guid UserId,
    Guid? AddedBy,
    DateTimeOffset AddedAt);

/// <summary>
/// DTO de OT habilitada por tenant (AC3 — HU-9776).
/// </summary>
public sealed record OtEnabledEntryDto(
    Guid Id,
    Guid CompanyId,
    string OtSlug,
    string ProcedureFamily,
    bool IsEnabled);
