namespace Flit.Modules.Identity.Application.DTOs;

/// <summary>DTO de permiso del catálogo global.</summary>
public sealed record PermissionDto(
    Guid Id,
    string Slug,
    string Module,
    string Action,
    string? Description);
