using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Interfaces;

namespace Flit.Modules.Identity.Application.Queries;

public sealed class ListUsersQueryHandler(IUserRepository userRepository)
{
    public async Task<PaginatedUsersDto> HandleAsync(
        ListUsersQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var (users, total) = await userRepository.ListByTenantPaginatedAsync(
            query.TenantId, page, pageSize, query.Search, ct);

        var items = (users ?? Array.Empty<User>()).Select(u => new UserListItemDto(
            Id: u.Id,
            Email: u.Email,
            FullName: u.FullName,
            Status: u.Status,
            MustResetPwd: u.MustResetPwd,
            LastLoginAt: u.LastLoginAt,
            CreatedAt: u.CreatedAt,
            Roles: u.UserRoles
                .Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Slug, ur.Role.Name))
                .OrderBy(r => r.Name)
                .ToArray()
        )).ToArray();

        return new PaginatedUsersDto(items, total, page, pageSize);
    }
}
