namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Agrega un usuario a la lista blanca de excepciones (bypass only_own_vehicles).
/// AC2 HU-9776 — POST /api/v1/admin/companies/{id}/user-exceptions
/// SuperAdmin only.
/// </summary>
public sealed record AddUserExceptionCommand(
    Guid CompanyId,
    Guid UserId,
    Guid AddedByUserId);
