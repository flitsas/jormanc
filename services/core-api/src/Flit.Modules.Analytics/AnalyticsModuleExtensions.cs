using Flit.Modules.Analytics.Application.Commands;
using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Domain.Interfaces;
using Flit.Modules.Analytics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.Analytics;

public static class AnalyticsModuleExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<GetDashboardSummaryQueryHandler>();
        services.AddScoped<GetDashboardProceduresQueryHandler>();
        services.AddScoped<GetDashboardTopUsersQueryHandler>();
        services.AddScoped<ExportDashboardExcelCommandHandler>();
        services.AddScoped<ExportDashboardPdfCommandHandler>();
        return services;
    }
}
