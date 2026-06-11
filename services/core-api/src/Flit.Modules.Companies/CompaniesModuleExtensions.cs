using Flit.Modules.Companies.Application.Commands;
using Flit.Modules.Companies.Application.Queries;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.Modules.Companies.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.Companies;

/// <summary>
/// Registro de servicios del módulo Companies en el contenedor DI.
/// Invoke desde Program.cs: services.AddCompaniesModule().
/// </summary>
public static class CompaniesModuleExtensions
{
    public static IServiceCollection AddCompaniesModule(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ITenantService, TenantService>();

        // Command handlers — HU-9774
        services.AddScoped<CreateCompanyCommandHandler>();
        services.AddScoped<UpdateCompanyConfigCommandHandler>();

        // Query handlers — HU-9774
        services.AddScoped<ListCompaniesQueryHandler>();

        // Command handlers — HU-9776 (Signature Matrix, User Exceptions, OT Enabled)
        services.AddScoped<UpdateSignatureMatrixCommandHandler>();
        services.AddScoped<AddUserExceptionCommandHandler>();
        services.AddScoped<RemoveUserExceptionCommandHandler>();
        services.AddScoped<UpdateOtEnabledCommandHandler>();

        // Query handlers — HU-9776
        services.AddScoped<GetSignatureMatrixQueryHandler>();
        services.AddScoped<GetUserExceptionsQueryHandler>();
        services.AddScoped<GetOtEnabledQueryHandler>();

        return services;
    }
}
