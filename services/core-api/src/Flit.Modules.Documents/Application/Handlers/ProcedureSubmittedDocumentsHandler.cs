using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Procedures.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Flit.Modules.Documents.Application.Handlers;

/// <summary>Consumer ProcedureSubmitted — pipeline documental HU-9791.</summary>
public sealed class ProcedureSubmittedDocumentsHandler(
    GenerateProcedureDocumentsCommandHandler generateHandler,
    ILogger<ProcedureSubmittedDocumentsHandler> logger)
{
    public async Task HandleAsync(ProcedureSubmitted evt, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Iniciando pipeline documental — ProcedureId={ProcedureId}",
            evt.ProcedureId);

        var result = await generateHandler.HandleAsync(
            new GenerateProcedureDocumentsCommand(evt.ProcedureId, evt.TenantId, Guid.Empty),
            ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Pipeline documental falló — ProcedureId={ProcedureId} Code={Code}",
                evt.ProcedureId, result.Error!.Code);
            return;
        }

        logger.LogInformation(
            "Pipeline documental completado — ProcedureId={ProcedureId} Consolidated={Consolidated}",
            evt.ProcedureId, result.Value);
    }
}
