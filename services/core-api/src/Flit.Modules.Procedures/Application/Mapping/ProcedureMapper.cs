using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Application.DTOs;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Application.Mapping;

public static class ProcedureMapper
{
    public static ProcedureDto ToDto(
        Procedure entity,
        IReadOnlyList<ProcedureStepSummaryDto> steps) =>
        new(
            Id: entity.Id,
            CompositeId: entity.CompositeId,
            Status: entity.Status,
            ProcedureTypeId: entity.ProcedureTypeId,
            ProcedureTypeSnapshotId: entity.ProcedureTypeSnapshotId,
            CompanyId: entity.CompanyId,
            OtId: entity.OtId,
            CurrentStepOrder: entity.CurrentStepOrder,
            Steps: steps,
            CreatedAt: entity.CreatedAt);

    public static ProcedureListItemDto ToListItem(Procedure entity) =>
        new(
            Id: entity.Id,
            CompositeId: entity.CompositeId,
            Status: entity.Status,
            ProcedureTypeId: entity.ProcedureTypeId,
            CompanyId: entity.CompanyId,
            CreatedAt: entity.CreatedAt);

    public static VehicleSummaryDto ToVehicleDto(VehicleSummary vehicle) =>
        new(
            Plate: vehicle.Plate,
            Vin: vehicle.Vin,
            Brand: vehicle.Brand,
            Model: vehicle.Model,
            Year: vehicle.Year,
            Color: vehicle.Color,
            OwnerName: vehicle.OwnerName);
}
