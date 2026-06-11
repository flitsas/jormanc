using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Handler de GetUserProfileQuery. Lee el perfil completo del usuario autenticado.
/// </summary>
public sealed class GetUserProfileQueryHandler(IUserRepository userRepository)
{
    public async Task<Result<UserProfileDto, IdentityError>> HandleAsync(
        GetUserProfileQuery query,
        CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdWithRolesAsync(query.UserId, query.TenantId, ct);

        if (user is null)
            return Result<UserProfileDto, IdentityError>.Failure(IdentityError.UserNotFound);

        var roles = user.UserRoles
            .Select(ur => ur.Role.Slug)
            .Distinct()
            .ToArray();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Slug)
            .Distinct()
            .ToArray();

        return Result<UserProfileDto, IdentityError>.Success(new UserProfileDto(
            Id: user.Id,
            Name: user.FullName,
            Email: user.Email,
            Roles: roles,
            Permissions: permissions,
            TenantId: user.TenantId,
            TenantName: user.Tenant.Name));
    }
}
