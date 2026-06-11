namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Query para listar los roles del tenant del JWT.
/// AC2 HU-9770: GET /api/v1/roles — aislamiento multi-tenant estricto.
/// </summary>
public sealed record ListRolesQuery(Guid TenantId);
