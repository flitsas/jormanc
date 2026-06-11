namespace Flit.Modules.OT.Application.Commands;

/// <summary>AC2 HU-9799 — DELETE labels con confirmación si hay adjuntos.</summary>
public sealed record DeleteOtLabelCommand(
    Guid OtId,
    Guid LabelId,
    Guid TenantId,
    Guid RequestedByUserId,
    bool Confirm);
