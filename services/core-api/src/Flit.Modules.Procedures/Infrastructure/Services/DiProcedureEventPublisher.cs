using Flit.Modules.Procedures.Application.Handlers;
using Flit.Modules.Procedures.Domain.Events;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

/// <summary>Publica eventos invocando handlers registrados en DI (HU-9786).</summary>
public sealed class DiProcedureEventPublisher(ProcedureSubmittedPipelineHandler handler) : IProcedureEventPublisher
{
    public Task PublishProcedureSubmittedAsync(ProcedureSubmitted evt, CancellationToken ct = default) =>
        handler.Handle(evt);
}
