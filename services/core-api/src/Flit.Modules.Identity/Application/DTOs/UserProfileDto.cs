namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>Perfil de usuario retornado en login y /auth/me.</summary>
public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string[] Roles,
    string[] Permissions,
    Guid TenantId,
    string TenantName);
