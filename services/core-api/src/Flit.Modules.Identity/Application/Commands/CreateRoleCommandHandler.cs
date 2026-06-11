using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de CreateRoleCommand.
/// Valida slug único en tenant, persiste rol + permisos asignados.
/// </summary>
public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IClock clock)
{
    public async Task<Result<RoleDto, IdentityError>> HandleAsync(
        CreateRoleCommand command, CancellationToken ct = default)
    {
        if (await roleRepository.SlugExistsAsync(command.Slug, command.TenantId, ct))
            return Result<RoleDto, IdentityError>.Failure(IdentityError.RoleSlugAlreadyExists);

        var permissions = await permissionRepository.GetByIdsAsync(command.PermissionIds, ct);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Slug = command.Slug,
            Name = command.Name,
            Description = command.Description,
            IsSystem = false,
            CreatedAt = clock.UtcNow,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = clock.UtcNow,
            UpdatedBy = command.RequestedByUserId,
            RolePermissions = permissions.Select(p => new RolePermission
            {
                PermissionId = p.Id,
                Permission = p
            }).ToList()
        };

        foreach (var rp in role.RolePermissions)
            rp.Role = role;

        await roleRepository.CreateAsync(role, ct);

        return Result<RoleDto, IdentityError>.Success(new RoleDto(
            Id: role.Id,
            Slug: role.Slug,
            Name: role.Name,
            Description: role.Description,
            IsSystem: role.IsSystem,
            Permissions: permissions.Select(p => p.Slug).ToArray()));
    }
}
