using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio de usuarios para el módulo Identity.
/// La implementación vive en Infrastructure y usa FlitDbContext.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario activo por email dentro del tenant identificado por slug.
    /// Incluye Tenant, UserRoles → Role → RolePermissions → Permission.
    /// </summary>
    Task<User?> FindByEmailAndTenantSlugAsync(
        string email, string tenantSlug, CancellationToken ct = default);

    /// <summary>
    /// Busca un usuario por Id y TenantId (para /auth/me post-autenticación).
    /// Incluye Tenant, UserRoles → Role → RolePermissions → Permission.
    /// </summary>
    Task<User?> FindByIdWithRolesAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default);

    // ── HU-9772 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si ya existe un usuario con ese email en el tenant indicado.
    /// Incluye usuarios con cualquier status (activo, pendiente, eliminado).
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Busca un usuario por email dentro del tenant (por ID).
    /// Usado en forgot-password cuando ya se conoce el tenant_id.
    /// </summary>
    Task<User?> FindByEmailAndTenantIdAsync(
        string email, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Persiste un nuevo usuario (status=active, asignado desde invitación).
    /// </summary>
    Task CreateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Persiste cambios a un usuario existente (p. ej. password_hash tras reset).
    /// EF Core rastrea el objeto; este método solo llama SaveChanges.
    /// </summary>
    Task UpdateAsync(CancellationToken ct = default);
}
