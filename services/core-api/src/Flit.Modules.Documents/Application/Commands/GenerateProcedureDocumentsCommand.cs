namespace Flit.Modules.Documents.Application.Commands;

public sealed record GenerateProcedureDocumentsCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId);
