namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Obtiene la lista blanca de excepciones de usuario (bypass only_own_vehicles).
/// AC2 HU-9776 — GET /api/v1/admin/companies/{id}/user-exceptions
/// </summary>
public sealed record GetUserExceptionsQuery(Guid CompanyId);
