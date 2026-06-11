using Flit.Modules.Procedures.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Flit.Modules.Procedures.Application.Handlers;

/// <summary>Consumer Wolverine — pipeline documental/firmas (stub HU-9786).</summary>
public sealed class ProcedureSubmittedPipelineHandler(ILogger<ProcedureSubmittedPipelineHandler> logger)
{
    public Task Handle(ProcedureSubmitted evt)
    {
        logger.LogInformation(
            "ProcedureSubmitted recibido — ProcedureId={ProcedureId} SnapshotId={SnapshotId}",
            evt.ProcedureId, evt.ProcedureTypeSnapshotId);
        return Task.CompletedTask;
    }
}
