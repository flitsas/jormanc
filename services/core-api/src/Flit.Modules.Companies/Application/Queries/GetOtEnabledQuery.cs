namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Obtiene todas las OTs habilitadas de una compañía por tenant.
/// AC3 HU-9776 — GET /api/v1/admin/companies/{id}/config/ot-enabled
/// </summary>
public sealed record GetOtEnabledQuery(Guid CompanyId);
