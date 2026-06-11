namespace Flit.Modules.OT.Application.Commands;

/// <summary>AC3 HU-9799 — POST labels con slug único por OT.</summary>
public sealed record CreateOtLabelCommand(
    Guid OtId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Slug,
    string DisplayName);
