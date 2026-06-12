using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Application.Queries;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Services;
using Flit.Modules.ProceduresConfig.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.ProceduresConfig;

public static class ProceduresConfigModuleExtensions
{
    public static IServiceCollection AddProceduresConfigModule(this IServiceCollection services)
    {
        services.AddScoped<IProcedureTypeRepository, ProcedureTypeRepository>();

        services.AddScoped<CreateProcedureTypeCommandHandler>();
        services.AddScoped<DeleteProcedureTypeCommandHandler>();
        services.AddScoped<CreateProcedureStepCommandHandler>();
        services.AddScoped<CreateFormSectionCommandHandler>();
        services.AddScoped<CreateFormFieldCommandHandler>();
        services.AddScoped<GetProcedureTypeQueryHandler>();

        // HU-9780 — Rule sets + CoherenceSimulator
        services.AddSingleton<ICoherenceSimulator, CoherenceSimulator>();
        services.AddScoped<CreateRuleSetCommandHandler>();
        services.AddScoped<SimulateCoherenceQueryHandler>();

        // HU-9781 — Actores, query-rules y vehicle-query
        services.AddScoped<CreateActorDefinitionCommandHandler>();
        services.AddScoped<CreateQueryRuleCommandHandler>();
        services.AddScoped<SetVehicleQueryKeyCommandHandler>();

        // HU-9782 — PUT steps, fields, api-connectors
        services.AddScoped<UpdateProcedureStepCommandHandler>();
        services.AddScoped<UpdateFormFieldCommandHandler>();
        services.AddScoped<CreateApiConnectorCommandHandler>();
        services.AddScoped<UpdateApiConnectorCommandHandler>();

        return services;
    }
}
