using Flit.Modules.Integrations.Application.Commands;
using Flit.Modules.Integrations.Application.Queries;
using Flit.Modules.Integrations.Application.UseCases;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Infrastructure;
using Flit.Modules.Integrations.Infrastructure.Connectors.Runt;
using Flit.Modules.Integrations.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA2000 // HttpClient lifecycle managed by IHttpClientFactory

namespace Flit.Modules.Integrations;

/// <summary>
/// Registro de servicios del módulo Integrations (ADR-0011 Strategy + hot-failover).
/// Invoke desde Program.cs: services.AddIntegrationsModule(config).
/// </summary>
public static class IntegrationsModuleExtensions
{
    public static IServiceCollection AddIntegrationsModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ─── Opciones de proveedores (credenciales desde env, nunca appsettings) ──
        services.Configure<VerifikOptions>(
            configuration.GetSection(VerifikOptions.SectionName));
        services.Configure<IntempoOptions>(
            configuration.GetSection(IntempoOptions.SectionName));

        // ─── HTTP Clients nombrados (timeouts gestionados por ConnectorRouter) ───
        services.AddHttpClient("verifik", (sp, c) =>
        {
            var opts = configuration.GetSection(VerifikOptions.SectionName)
                .Get<VerifikOptions>() ?? new VerifikOptions();
            c.BaseAddress = new Uri(opts.BaseUrl);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("intempo", (sp, c) =>
        {
            var opts = configuration.GetSection(IntempoOptions.SectionName)
                .Get<IntempoOptions>() ?? new IntempoOptions();
            c.BaseAddress = new Uri(opts.BaseUrl);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ─── Connectors RUNT registrados como IRuntConnector — ConnectorRouter los resuelve por ProviderName ─
        services.AddScoped<IRuntConnector, VerifikRuntConnector>();
        services.AddScoped<IRuntConnector, IntempoRuntConnector>();
        services.AddScoped<IRuntConnector, MockRuntConnector>();

        // ─── ConnectorRouter — orquesta failover y log de payloads ──────────────
        services.AddScoped<ConnectorRouter>();

        // ─── Repositories ────────────────────────────────────────────────────────
        services.AddScoped<IConnectorConfigRepository, ConnectorConfigRepository>();
        services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();

        // ─── Use cases HU-9775 ────────────────────────────────────────────────────
        services.AddScoped<QueryVehicleByPlateUseCase>();

        // ─── Command handlers ────────────────────────────────────────────────────
        services.AddScoped<UpsertConnectorConfigCommandHandler>();

        // ─── Query handlers ───────────────────────────────────────────────────────
        services.AddScoped<GetConnectorConfigQueryHandler>();
        services.AddScoped<GetIntegrationLogsQueryHandler>();

        return services;
    }
}
