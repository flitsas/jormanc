namespace Flit.Modules.Documents.Application.Commands;

public sealed record ForceReconsolidationCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId);
