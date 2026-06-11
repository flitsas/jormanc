namespace Flit.Modules.Companies.Domain.Errors;

/// <summary>
/// Errores de dominio del módulo Companies.
/// Cada instancia es un error tipado (Result pattern — ADR-0002 §8.1).
/// </summary>
public sealed record CompanyError(string Code, string Message)
{
    public static readonly CompanyError NitAlreadyExists =
        new("COMPANY_NIT_ALREADY_EXISTS", "Ya existe una compañía con ese NIT.");

    public static readonly CompanyError TenantSlugAlreadyExists =
        new("COMPANY_TENANT_SLUG_ALREADY_EXISTS", "El slug de tenant ya está en uso.");

    public static readonly CompanyError NotFound =
        new("COMPANY_NOT_FOUND", "Compañía no encontrada.");

    public static readonly CompanyError ConfigNotFound =
        new("COMPANY_CONFIG_NOT_FOUND", "Configuración de compañía no encontrada.");

    public static readonly CompanyError InvalidStatus =
        new("COMPANY_INVALID_STATUS", "Estado de compañía inválido. Valores permitidos: active, inactive, suspended.");

    public static readonly CompanyError InvalidNotificationTarget =
        new("COMPANY_INVALID_NOTIFICATION_TARGET", "notification_target inválido. Valores permitidos: comprador, radicador, ninguno.");

    public static readonly CompanyError InvalidSmtpMode =
        new("COMPANY_INVALID_SMTP_MODE", "smtp_mode inválido. Valores permitidos: native, api_cliente.");

    public static readonly CompanyError InvalidActorRole =
        new("COMPANY_INVALID_ACTOR_ROLE", "actor_role inválido. Valores permitidos: vendedor, comprador, representante_legal.");

    public static readonly CompanyError InvalidSignatureType =
        new("COMPANY_INVALID_SIGNATURE_TYPE", "signature_type inválido. Valores permitidos: identidad_digital, firma_pantalla, preasignada.");

    public static readonly CompanyError InvalidProcedureFamily =
        new("COMPANY_INVALID_PROCEDURE_FAMILY", "procedure_family inválido. Valores permitidos: matricula_inicial, traspasos, otros.");

    public static readonly CompanyError UserExceptionAlreadyExists =
        new("COMPANY_USER_EXCEPTION_ALREADY_EXISTS", "El usuario ya está en la lista blanca de esta compañía.");

    public static readonly CompanyError UserExceptionNotFound =
        new("COMPANY_USER_EXCEPTION_NOT_FOUND", "Excepción de usuario no encontrada.");
}
