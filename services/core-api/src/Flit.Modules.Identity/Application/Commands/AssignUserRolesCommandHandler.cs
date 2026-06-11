using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de AssignUserRolesCommand.
/// Reemplaza los roles del usuario, revoca todas sus sesiones activas
/// y notifica al cliente vía SignalR con el evento "SessionRevoked" (AC2 HU-9771).
/// </summary>
public sealed class AssignUserRolesCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISessionRepository sessionRepository,
    ISessionBlacklist sessionBlacklist,
    ISessionNotifier sessionNotifier,
    IClock clock)
{
    public async Task<Result<AssignUserRolesResult, IdentityError>> HandleAsync(
        AssignUserRolesCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdWithRolesAsync(command.UserId, command.TenantId, ct);
        if (user is null)
            return Result<AssignUserRolesResult, IdentityError>.Failure(IdentityError.UserNotFound);

        var newRoles = await roleRepository.GetByIdsAsync(command.RoleIds, command.TenantId, ct);

        user.UserRoles.Clear();
        foreach (var role in newRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = command.TenantId,
                AssignedAt = clock.UtcNow,
                AssignedBy = command.RequestedByUserId
            });
        }

        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = command.RequestedByUserId;

        // Revoke all active sessions → JTIs enter the IMemoryCache blacklist immediately (AC1)
        var activeSessions = await sessionRepository.GetActiveByUserIdAsync(command.UserId, ct);
        foreach (var session in activeSessions)
        {
            await sessionRepository.RevokeAsync(session, command.RequestedByUserId, clock.UtcNow, ct);
            await sessionBlacklist.RevokeAsync(session.Jti, session.ExpiresAt, ct);
        }

        // Push SignalR event to user's channel so the frontend reacts immediately (AC2)
        if (activeSessions.Count > 0)
            await sessionNotifier.NotifySessionRevokedAsync(command.UserId, "roles_changed", ct);

        return Result<AssignUserRolesResult, IdentityError>.Success(new AssignUserRolesResult(
            UserId: user.Id,
            Roles: newRoles.Select(r => new RoleSummaryDto(r.Id, r.Slug, r.Name)).ToArray()));
    }
}

/// <summary>Resultado de PATCH /users/{userId}/roles.</summary>
public sealed record AssignUserRolesResult(
    Guid UserId,
    RoleSummaryDto[] Roles);
