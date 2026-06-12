namespace Flit.Modules.Identity.Application.Queries;

/// <summary>GET /api/v1/users — listado paginado del tenant (HU-9773 / diseño #9567).</summary>
public sealed record ListUsersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null);
