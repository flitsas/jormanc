using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Application.Handlers;
using Flit.Modules.Documents.Application.Queries;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Flit.Modules.Documents.Infrastructure.Persistence;
using Flit.Modules.Documents.Infrastructure.Storage;
using Flit.Modules.Documents.Infrastructure.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Flit.Modules.Documents;

public static class DocumentsModuleExtensions
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IDocumentTemplateStorage, LocalDocumentTemplateStorage>();
        services.AddSingleton<IDocumentFileStorage, LocalDocumentFileStorage>();
        services.AddSingleton<ITemplateResolver, TemplateResolver>();
        services.AddSingleton<IDocumentPdfRenderer, QuestPdfDocumentRenderer>();
        services.AddSingleton<IPdfMerger, PdfSharpMerger>();

        services.AddScoped<CreateDocumentTypeCommandHandler>();
        services.AddScoped<AssociateDocumentToProcedureTypeCommandHandler>();
        services.AddScoped<GetProcedureTypeDocumentConfigQueryHandler>();
        services.AddScoped<CreateDocumentTemplateCommandHandler>();
        services.AddScoped<ListDocumentTemplatesQueryHandler>();
        services.AddScoped<GetDocumentTemplateQueryHandler>();
        services.AddScoped<GenerateTemplatePreviewPdfCommandHandler>();

        services.AddScoped<GenerateProcedureDocumentsCommandHandler>();
        services.AddScoped<ConsolidateDocumentsCommandHandler>();
        services.AddScoped<ForceReconsolidationCommandHandler>();
        services.AddScoped<ProcedureSubmittedDocumentsHandler>();
        services.AddScoped<GetProcedureDocumentsQueryHandler>();
        services.AddScoped<GetConsolidatedPackageQueryHandler>();

        return services;
    }
}
