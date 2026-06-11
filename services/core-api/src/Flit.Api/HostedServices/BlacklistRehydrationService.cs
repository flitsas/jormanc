using Flit.Modules.Identity.Infrastructure;

namespace Flit.Api.HostedServices;

/// <summary>
/// Hosted service que rehydrata la blacklist de JTIs al arrancar el proceso.
/// Delega la lógica a BlacklistRehydrator (scoped) obtenido vía IServiceScopeFactory.
/// Mitiga la pérdida de blacklist ante reinicios (ADR-0013).
/// </summary>
public sealed class BlacklistRehydrationService(
    IServiceScopeFactory scopeFactory,
    ILogger<BlacklistRehydrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var rehydrator = scope.ServiceProvider.GetRequiredService<BlacklistRehydrator>();

        var count = await rehydrator.RehydrateAsync(cancellationToken);

        logger.LogInformation(
            "Blacklist rehydrated: {Count} JTI(s) revocados cargados en IMemoryCache",
            count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
