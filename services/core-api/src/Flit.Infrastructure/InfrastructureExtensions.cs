using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Repositories;

namespace Flit.Infrastructure;

/// <summary>
/// Extension de IServiceCollection para registrar EF Core + repositorios FLIT 2.0.
/// Invocado desde Program.cs cuando ConnectionStrings:Core esta configurado.
///
/// FLIT 2.0:
///   - Identity (Users, PasswordResetTokens, UserAuditLog, SyncInconsistencies)
///   - RBAC (Roles, Permissions, MenuItems, joins)
///   - Notifications (NotificationDelivery — dominio, no senders)
///   - Identity LEGACY (Usuario, IdentityCredential, RefreshTokenEntry) hasta Fase 7
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Registra FlitDbContext (Npgsql) y los repositorios EF Core
    /// disponibles. Los repositorios EF son Scoped (DbContext es Scoped).
    /// </summary>
    public static IServiceCollection AddPostgresInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FlitDbContext>(opts =>
            opts.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                })
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false));

        // ── Users (ADR-0010) ───────────────────────────────────────────
        services.AddScoped<Flit.Modules.Users.Ports.IUsersRepository, EfUsersRepository>();
        services.AddScoped<Flit.Modules.Users.Application.IUnitOfWork, EfUnitOfWork>();

        // ── Auth (Fase 7 — Hybrid Cognito + MFA) ───────────────────────
        services.AddScoped<Flit.Modules.Auth.Application.IPasswordResetTokensRepository,
            EfPasswordResetTokensRepository>();

        // ── RBAC (ADR-0011) ────────────────────────────────────────────
        services.AddScoped<Flit.Modules.Rbac.Ports.IRolesRepository, EfRolesRepository>();
        services.AddScoped<Flit.Modules.Rbac.Ports.IPermissionsRepository, EfPermissionsRepository>();
        services.AddScoped<Flit.Modules.Rbac.Ports.IMenuItemsRepository, EfMenuItemsRepository>();
        services.AddScoped<Flit.Modules.Rbac.Ports.IRoleAssignmentsRepository, EfRoleAssignmentsRepository>();

        // ── Notifications ──────────────────────────────────────────────
        services.AddScoped<Flit.Modules.Notifications.Ports.INotificationsRepository,
            EfNotificationsRepository>();

        // ── Identity LEGACY (eliminar en Fase 7 con HybridCognito) ────
        services.AddScoped<Flit.Modules.Identity.Ports.IUsuariosRepository, EfUsuariosRepository>();
        services.AddScoped<Flit.Modules.Identity.Ports.ICredentialsRepository, EfCredentialsRepository>();
        services.AddScoped<Flit.Modules.Identity.Ports.IRefreshTokenStore, EfRefreshTokenStore>();

        // ── Procedures Config — RGL-02 (#9438) + RGL-03 (#9439) ────────
        services.AddScoped<Flit.Modules.ProceduresConfig.Ports.IProcedureRulesRepository,
            NpgsqlProcedureRulesRepository>();
        services.AddScoped<Flit.Modules.ProceduresConfig.Ports.IEndpointCallLogRepository,
            NpgsqlEndpointCallLogRepository>();
        services.AddScoped<Flit.Modules.ProceduresConfig.Ports.IEndpointCatalogRepository,
            NpgsqlEndpointCatalogRepository>();
        services.AddScoped<Flit.Modules.ProceduresConfig.Ports.IRuleEndpointInvoker,
            Flit.Modules.ProceduresConfig.Application.CatalogRuleEndpointInvoker>();

        return services;
    }
}
