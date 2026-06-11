namespace Flit.Modules.Procedures.Application.Queries;

/// <summary>AC3 HU-9784 — GET /procedures</summary>
public sealed record ListProceduresQuery(
    Guid TenantId,
    string? Status = null,
    DateTimeOffset? DateFrom = null,
    int Page = 1,
    int PageSize = 20);
