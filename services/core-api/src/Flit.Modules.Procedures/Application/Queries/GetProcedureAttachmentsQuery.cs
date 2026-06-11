namespace Flit.Modules.Procedures.Application.Queries;

public sealed record GetProcedureAttachmentsQuery(Guid ProcedureId, Guid TenantId);
