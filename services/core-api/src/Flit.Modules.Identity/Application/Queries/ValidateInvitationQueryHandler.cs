using System.Text.Json;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Queries;

/// <summary>
/// Handler de ValidateInvitationQuery.
/// Verifica que el token existe, no ha expirado y está pendiente.
/// AC2 de HU-9772: token expirado → INVITATION_EXPIRED.
/// </summary>
public sealed class ValidateInvitationQueryHandler(
    IInvitationRepository invitationRepository,
    IRoleRepository roleRepository,
    IClock clock)
{
    public async Task<Result<InvitationValidateDto, IdentityError>> HandleAsync(
        ValidateInvitationQuery query,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHelper.Hash(query.Token);
        var invitation = await invitationRepository.FindByTokenHashAsync(tokenHash, ct);

        if (invitation is null)
            return Result<InvitationValidateDto, IdentityError>.Failure(IdentityError.InvitationNotFound);

        if (invitation.ExpiresAt <= clock.UtcNow)
            return Result<InvitationValidateDto, IdentityError>.Failure(IdentityError.InvitationExpired);

        if (invitation.Status != "pending")
            return Result<InvitationValidateDto, IdentityError>.Failure(IdentityError.InvitationAlreadyUsed);

        var roleIds = JsonSerializer.Deserialize<Guid[]>(invitation.RolesJson) ?? [];
        var roles = await roleRepository.GetByIdsAsync(roleIds, invitation.TenantId, ct);
        var roleSlugs = roles.Select(r => r.Slug).ToArray();

        return Result<InvitationValidateDto, IdentityError>.Success(new InvitationValidateDto(
            Email: invitation.Email,
            TenantId: invitation.TenantId,
            TenantName: invitation.Tenant.Name,
            RoleSlugs: roleSlugs));
    }
}
