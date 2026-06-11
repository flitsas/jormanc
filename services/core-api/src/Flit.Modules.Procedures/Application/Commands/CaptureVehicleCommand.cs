namespace Flit.Modules.Procedures.Application.Commands;

/// <summary>AC2 HU-9784 — PATCH /procedures/{id}/vehicle</summary>
public sealed record CaptureVehicleCommand(
    Guid ProcedureId,
    Guid TenantId,
    Guid UserId,
    string? Plate,
    string? Vin);
