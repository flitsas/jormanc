namespace Flit.Modules.Documents.Application.Queries;

public sealed record GetProcedureDocumentsQuery(Guid ProcedureId, Guid TenantId);
