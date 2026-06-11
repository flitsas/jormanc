namespace Flit.Modules.Procedures.Application.Commands;

/// <summary>AC1 HU-9784 — POST /procedures</summary>
public sealed record CreateProcedureCommand(
    Guid TenantId,
    Guid UserId,
    Guid ProcedureTypeId,
    Guid CompanyId,
    Guid? OtId = null);
