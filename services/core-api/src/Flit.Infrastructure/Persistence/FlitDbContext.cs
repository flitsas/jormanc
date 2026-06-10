using Flit.Infrastructure.Persistence.Joins;
using Flit.Modules.Identity.Domain;
using Flit.Modules.Notifications.Domain;
using Flit.Modules.Rbac.Domain;
using Flit.Modules.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Flit.Infrastructure.Persistence;

/// <summary>
/// DbContext unificado del Modular Monolith FLIT 2.0 (shell post-reset trámites).
///
/// Schemas activos:
///   identity       — users, password_reset_tokens, user_audit_log, sync_inconsistencies
///   rbac           — roles, permissions, menu_items, joins
///   notifications  — notification_delivery
///
/// Identity LEGACY (Usuario, IdentityCredential, RefreshTokenEntry) hasta Fase 7.
/// </summary>
public sealed class FlitDbContext(DbContextOptions<FlitDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserAuditLog> UserAuditLogs => Set<UserAuditLog>();
    public DbSet<SyncInconsistency> SyncInconsistencies => Set<SyncInconsistency>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<UserRoleEntity> UserRoleAssignments => Set<UserRoleEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<RoleMenuItemEntity> RoleMenuItems => Set<RoleMenuItemEntity>();

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<IdentityCredential> Credenciales => Set<IdentityCredential>();
    public DbSet<RefreshTokenEntry> RefreshTokens => Set<RefreshTokenEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlitDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
