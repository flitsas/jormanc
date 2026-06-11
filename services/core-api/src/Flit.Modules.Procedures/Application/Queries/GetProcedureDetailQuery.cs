namespace Flit.Modules.Procedures.Application.Queries;

/// <summary>GET /procedures/{id} — detalle con snapshot config (HU-9787).</summary>
public sealed record GetProcedureDetailQuery(Guid ProcedureId, Guid TenantId);
