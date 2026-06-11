namespace Flit.Modules.Procedures.Application.Commands;

/// <summary>AC1-AC2 HU-9786 — POST /procedures/{id}/submit</summary>
public sealed record SubmitProcedureCommand(Guid ProcedureId, Guid TenantId, Guid UserId);
