using Flit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Infrastructure;

/// <summary>
/// Extension de IServiceCollection para registrar EF Core (Npgsql) en FLIT 2.0.
/// Invocado desde Program.cs cuando ConnectionStrings:Core esta configurado.
///
/// Esqueleto base post-reset: solo registra el FlitDbContext (vacio).
/// Las nuevas features agregan aqui sus repositorios al implementarse.
/// </summary>
public static class InfrastructureExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<TenantConnectionInterceptor>();
        services.AddSingleton<TenantCommandInterceptor>();

        services.AddDbContext<FlitDbContext>((sp, opts) =>
            opts.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                })
            .AddInterceptors(
                sp.GetRequiredService<TenantConnectionInterceptor>(),
                sp.GetRequiredService<TenantCommandInterceptor>())
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false));

        return services;
    }
}
