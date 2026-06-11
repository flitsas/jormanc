using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de AcceptInvitationCommand.
/// Verifica token, crea usuario activo con roles, emite JWT.
/// AC1 de HU-9772.
/// </summary>
public sealed class AcceptInvitationCommandHandler(
    IInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IClock clock)
{
    public async Task<Result<TokenDto, IdentityError>> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHelper.Hash(command.Token);
        var invitation = await invitationRepository.FindByTokenHashAsync(tokenHash, ct);

        if (invitation is null)
            return Result<TokenDto, IdentityError>.Failure(IdentityError.InvitationNotFound);

        if (invitation.ExpiresAt <= clock.UtcNow)
            return Result<TokenDto, IdentityError>.Failure(IdentityError.InvitationExpired);

        if (invitation.Status != "pending")
            return Result<TokenDto, IdentityError>.Failure(IdentityError.InvitationAlreadyUsed);

        var roleIds = JsonSerializer.Deserialize<Guid[]>(invitation.RolesJson) ?? [];
        var roles = await roleRepository.GetByIdsAsync(roleIds, invitation.TenantId, ct);

        var passwordHash = passwordHasher.Hash(command.Password);

        var now = clock.UtcNow;
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            TenantId = invitation.TenantId,
            Email = invitation.Email,
            FullName = command.FullName,
            PasswordHash = passwordHash,
            Status = "active",
            MustResetPwd = false,
            CreatedAt = now,
            CreatedBy = invitation.InvitedBy ?? Guid.Empty,
            UpdatedAt = now,
            UpdatedBy = invitation.InvitedBy ?? Guid.Empty
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.Id,
                TenantId = invitation.TenantId,
                AssignedAt = now,
                AssignedBy = invitation.InvitedBy
            });
        }

        await userRepository.CreateAsync(user, ct);

        invitation.Status = "accepted";
        invitation.AcceptedAt = now;
        await invitationRepository.UpdateAsync(invitation, ct);

        // Load tenant for JWT claims
        var createdUser = await userRepository.FindByIdWithRolesAsync(userId, invitation.TenantId, ct);
        if (createdUser is null)
            return Result<TokenDto, IdentityError>.Failure(IdentityError.UserNotFound);

        var roleSlugs = createdUser.UserRoles.Select(ur => ur.Role.Slug).Distinct().ToArray();
        var permissions = createdUser.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Slug)
            .Distinct()
            .ToArray();

        var tokenResult = tokenIssuer.Issue(new TokenRequest(
            UserId: createdUser.Id,
            TenantId: createdUser.TenantId,
            TenantName: createdUser.Tenant.Name,
            FullName: createdUser.FullName,
            Email: createdUser.Email,
            Roles: roleSlugs,
            Permissions: permissions));

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = createdUser.Id,
            TenantId = createdUser.TenantId,
            Jti = tokenResult.Jti,
            ExpiresAt = tokenResult.ExpiresAt,
            IsRevoked = false,
            CreatedAt = now
        };
        await sessionRepository.CreateAsync(session, ct);

        var profile = new UserProfileDto(
            Id: createdUser.Id,
            Name: createdUser.FullName,
            Email: createdUser.Email,
            Roles: roleSlugs,
            Permissions: permissions,
            TenantId: createdUser.TenantId,
            TenantName: createdUser.Tenant.Name);

        return Result<TokenDto, IdentityError>.Success(
            new TokenDto(tokenResult.AccessToken, tokenResult.ExpiresIn, profile));
    }
}
