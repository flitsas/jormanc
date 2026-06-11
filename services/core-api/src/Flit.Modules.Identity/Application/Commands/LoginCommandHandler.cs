using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler del LoginCommand. Valida credenciales, emite JWT RS256 y registra sesión.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IClock clock)
{
    public async Task<Result<TokenDto, IdentityError>> HandleAsync(
        LoginCommand command,
        CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailAndTenantSlugAsync(
            command.Email, command.TenantSlug, ct);

        // AC2: contraseña incorrecta o usuario/tenant no existe → mismo error (no revelar existencia)
        if (user is null)
            return Result<TokenDto, IdentityError>.Failure(IdentityError.InvalidCredentials);

        if (user.Status != "active")
            return Result<TokenDto, IdentityError>.Failure(IdentityError.UserNotActive);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result<TokenDto, IdentityError>.Failure(IdentityError.InvalidCredentials);

        var roles = ExtractRoles(user);
        var permissions = ExtractPermissions(user);

        var tokenResult = tokenIssuer.Issue(new TokenRequest(
            UserId: user.Id,
            TenantId: user.TenantId,
            TenantName: user.Tenant.Name,
            FullName: user.FullName,
            Email: user.Email,
            Roles: roles,
            Permissions: permissions));

        // AC1: registrar sesión en identity.sessions
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            Jti = tokenResult.Jti,
            ExpiresAt = tokenResult.ExpiresAt,
            IsRevoked = false,
            CreatedAt = clock.UtcNow
        };
        await sessionRepository.CreateAsync(session, ct);

        var profile = new UserProfileDto(
            Id: user.Id,
            Name: user.FullName,
            Email: user.Email,
            Roles: roles,
            Permissions: permissions,
            TenantId: user.TenantId,
            TenantName: user.Tenant.Name);

        return Result<TokenDto, IdentityError>.Success(
            new TokenDto(tokenResult.AccessToken, tokenResult.ExpiresIn, profile));
    }

    private static string[] ExtractRoles(User user) =>
        user.UserRoles
            .Select(ur => ur.Role.Slug)
            .Distinct()
            .ToArray();

    private static string[] ExtractPermissions(User user) =>
        user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Slug)
            .Distinct()
            .ToArray();
}
