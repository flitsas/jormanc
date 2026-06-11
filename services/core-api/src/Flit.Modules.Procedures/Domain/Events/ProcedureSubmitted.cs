namespace Flit.Modules.Procedures.Domain.Events;

/// <summary>Evento Wolverine disparado al someter un trámite (HU-9786).</summary>
public sealed record ProcedureSubmitted(
    Guid ProcedureId,
    Guid TenantId,
    Guid ProcedureTypeSnapshotId,
    DateTimeOffset SubmittedAt);
