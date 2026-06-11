namespace Flit.Modules.OT.Application.Commands;

/// <summary>AC1 HU-9799 — UPSERT prelación documental (&lt; 500ms).</summary>
public sealed record UpdateDocumentOrderCommand(
    Guid OtId,
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    IReadOnlyList<Guid> OrderedDocumentTypeIds);
