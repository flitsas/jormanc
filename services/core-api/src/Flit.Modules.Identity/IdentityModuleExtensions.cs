using Flit.Modules.Identity.Application.Commands;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.Modules.Identity.Infrastructure;
using Flit.Modules.Identity.Infrastructure.Email;
using Flit.Modules.Identity.Infrastructure.Persistence;
using Flit.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.Identity;

/// <summary>
/// Registro de servicios del módulo Identity en el contenedor DI.
/// Invoke desde Program.cs: services.AddIdentityModule(configuration, keyProvider).
/// </summary>
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        RsaKeyProvider keyProvider)
    {
        // JWT options
        services.Configure<IdentityJwtOptions>(
            configuration.GetSection(IdentityJwtOptions.SectionName));

        // Clave RSA compartida (creada manualmente en Program.cs antes del build)
        services.AddSingleton(keyProvider);

        // Security services
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<ISessionBlacklist, InMemorySessionBlacklist>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // Email sender (dev: console/log; prod: swap to real SMTP sender)
        services.AddSingleton<ConsoleEmailSender>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<ConsoleEmailSender>());

        // Repositories (scoped — ciclo de vida del request)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Infrastructure — rehydration helper (scoped: necesita ISessionRepository scoped)
        services.AddScoped<BlacklistRehydrator>();

        // Command / Query handlers (scoped)
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetUserProfileQueryHandler>();

        // HU-9770 handlers
        services.AddScoped<CreateRoleCommandHandler>();
        services.AddScoped<DeleteRoleCommandHandler>();
        services.AddScoped<AssignUserRolesCommandHandler>();
        services.AddScoped<ListRolesQueryHandler>();
        services.AddScoped<ListUsersQueryHandler>();
        services.AddScoped<ListPermissionsQueryHandler>();

        // HU-9772 handlers
        services.AddScoped<CreateInvitationCommandHandler>();
        services.AddScoped<AcceptInvitationCommandHandler>();
        services.AddScoped<ValidateInvitationQueryHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        return services;
    }
}
