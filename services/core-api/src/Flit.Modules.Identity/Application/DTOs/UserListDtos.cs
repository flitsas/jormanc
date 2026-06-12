namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>Usuario en listado paginado GET /users.</summary>
public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    bool MustResetPwd,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    RoleSummaryDto[] Roles);

/// <summary>Respuesta paginada GET /users.</summary>
public sealed record PaginatedUsersDto(
    UserListItemDto[] Items,
    int Total,
    int Page,
    int PageSize);
