using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de DeleteRoleCommand.
/// AC3: si is_system=true → SYSTEM_ROLE_CANNOT_BE_DELETED (409).
/// </summary>
public sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository)
{
    public async Task<Result<bool, IdentityError>> HandleAsync(
        DeleteRoleCommand command, CancellationToken ct = default)
    {
        var role = await roleRepository.FindByIdAsync(command.RoleId, command.TenantId, ct);

        if (role is null)
            return Result<bool, IdentityError>.Failure(IdentityError.RoleNotFound);

        if (role.IsSystem)
            return Result<bool, IdentityError>.Failure(IdentityError.SystemRoleCannotBeDeleted);

        await roleRepository.DeleteAsync(role, ct);

        return Result<bool, IdentityError>.Success(true);
    }
}
