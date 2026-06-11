using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Application.Handlers;
using Flit.Modules.Procedures.Application.Queries;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.Procedures.Infrastructure.Persistence;
using Flit.Modules.Procedures.Infrastructure.Services;
using Flit.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.Procedures;

public static class ProceduresModuleExtensions
{
    public static IServiceCollection AddProceduresModule(this IServiceCollection services)
    {
        services.AddScoped<IProcedureRepository, ProcedureRepository>();
        services.AddScoped<IProcedureStatusUpdater, ProcedureStatusUpdater>();
        services.AddScoped<IVehicleQueryService, VehicleQueryService>();
        services.AddScoped<IPersonQueryService, PersonQueryService>();
        services.AddScoped<ILegalEntityQueryService, LegalEntityQueryService>();

        services.AddScoped<CreateProcedureCommandHandler>();
        services.AddScoped<CaptureVehicleCommandHandler>();
        services.AddScoped<AddActorCommandHandler>();
        services.AddScoped<SubmitProcedureCommandHandler>();
        services.AddScoped<UploadAttachmentCommandHandler>();
        services.AddScoped<ListProceduresQueryHandler>();
        services.AddScoped<GetProcedureDetailQueryHandler>();
        services.AddScoped<GetProcedureAttachmentsQueryHandler>();
        services.AddScoped<GetSecondarySellersQueryHandler>();
        services.AddScoped<ProcedureSubmittedPipelineHandler>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }
}
