using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Application.Queries;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.Modules.OT.Infrastructure.Persistence;
using Flit.Modules.OT.Infrastructure.Webhooks;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.OT;

public static class OtModuleExtensions
{
    public static IServiceCollection AddOtModule(this IServiceCollection services)
    {
        services.AddScoped<IOtOrganismRepository, OtOrganismRepository>();
        services.AddScoped<IOtIntegrationLogRepository, OtIntegrationLogRepository>();
        services.AddSingleton<IQuipuxWebhookValidator, QuipuxWebhookValidator>();
        services.AddScoped<IOtDocumentOrderRepository, OtDocumentOrderRepository>();
        services.AddScoped<IOtDocumentLabelRepository, OtDocumentLabelRepository>();
        services.AddScoped<IOtDocumentTypeLookup, OtDocumentTypeLookup>();

        services.AddScoped<CreateOtOrganismCommandHandler>();
        services.AddScoped<UpdateOtOrganismCommandHandler>();
        services.AddScoped<DeleteOtOrganismCommandHandler>();
        services.AddScoped<UpdateOtModeCommandHandler>();
        services.AddScoped<UpdateQuipuxConfigCommandHandler>();
        services.AddScoped<UpdateDocumentOrderCommandHandler>();
        services.AddScoped<CreateOtLabelCommandHandler>();
        services.AddScoped<UpdateOtLabelCommandHandler>();
        services.AddScoped<DeleteOtLabelCommandHandler>();
        services.AddScoped<ProcessQuipuxWebhookCommandHandler>();

        services.AddScoped<ListOtOrganismsQueryHandler>();
        services.AddScoped<GetOtOrganismQueryHandler>();
        services.AddScoped<GetDocumentOrderQueryHandler>();
        services.AddScoped<GetOtLabelsQueryHandler>();
        services.AddScoped<GetOtLabelImpactQueryHandler>();
        services.AddScoped<GetOtIntegrationLogsQueryHandler>();

        return services;
    }
}
