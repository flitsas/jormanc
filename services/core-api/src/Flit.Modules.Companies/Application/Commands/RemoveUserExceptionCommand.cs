namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Elimina un usuario de la lista blanca de excepciones (bypass only_own_vehicles).
/// AC2 HU-9776 — DELETE /api/v1/admin/companies/{id}/user-exceptions/{userId}
/// SuperAdmin only.
/// </summary>
public sealed record RemoveUserExceptionCommand(
    Guid CompanyId,
    Guid UserId,
    Guid RequestedByUserId);
