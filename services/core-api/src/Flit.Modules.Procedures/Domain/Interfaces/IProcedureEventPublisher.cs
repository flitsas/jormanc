using Flit.Modules.Procedures.Domain.Events;

namespace Flit.Modules.Procedures.Domain.Interfaces;

public interface IProcedureEventPublisher
{
    Task PublishProcedureSubmittedAsync(ProcedureSubmitted evt, CancellationToken ct = default);
}
