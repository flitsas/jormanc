namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Crea una compañía nueva + tenant en identity.tenants + company_config por defecto.
/// AC1 HU-9774.
/// </summary>
public sealed record CreateCompanyCommand(
    string Nit,
    string Name,
    string TenantSlug,
    /// <summary>ID del SuperAdmin que ejecuta la acción (del JWT).</summary>
    Guid RequestedByUserId);
