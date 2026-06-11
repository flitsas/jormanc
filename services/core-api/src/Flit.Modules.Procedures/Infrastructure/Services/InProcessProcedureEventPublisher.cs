using Flit.Modules.Procedures.Domain.Events;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

/// <summary>Publisher en memoria para tests y fallback sin Wolverine host.</summary>
public sealed class InProcessProcedureEventPublisher : IProcedureEventPublisher
{
    public static readonly List<ProcedureSubmitted> Published = [];

    public Task PublishProcedureSubmittedAsync(ProcedureSubmitted evt, CancellationToken ct = default)
    {
        Published.Add(evt);
        return Task.CompletedTask;
    }
}
