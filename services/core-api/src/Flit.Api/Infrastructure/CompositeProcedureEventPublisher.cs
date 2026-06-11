using Flit.Modules.Documents.Application.Handlers;
using Flit.Modules.Procedures.Application.Handlers;
using Flit.Modules.Procedures.Domain.Events;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Api.Infrastructure;

/// <summary>Orquesta consumers de ProcedureSubmitted entre módulos (HU-9786 + HU-9791).</summary>
public sealed class CompositeProcedureEventPublisher(
    ProcedureSubmittedPipelineHandler proceduresHandler,
    ProcedureSubmittedDocumentsHandler documentsHandler) : IProcedureEventPublisher
{
    public async Task PublishProcedureSubmittedAsync(ProcedureSubmitted evt, CancellationToken ct = default)
    {
        await proceduresHandler.Handle(evt);
        await documentsHandler.HandleAsync(evt, ct);
    }
}
