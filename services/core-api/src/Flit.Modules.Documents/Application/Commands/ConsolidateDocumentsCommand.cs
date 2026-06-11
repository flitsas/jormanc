namespace Flit.Modules.Documents.Application.Commands;

public sealed record ConsolidateDocumentsCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId,
    bool Force = false);
