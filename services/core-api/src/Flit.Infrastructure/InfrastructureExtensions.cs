using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Flit.Infrastructure.Persistence;

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

        return services;
    }
}
