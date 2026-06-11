namespace Flit.Modules.Procedures.Application.Commands;

/// <summary>AC1-AC3 HU-9785 — POST /procedures/{id}/actors</summary>
public sealed record AddActorCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId,
    Guid ActorDefinitionId,
    string Nature,
    string? DocumentType,
    string? DocumentNumber,
    string? Nit,
    decimal? CuotaPct);
