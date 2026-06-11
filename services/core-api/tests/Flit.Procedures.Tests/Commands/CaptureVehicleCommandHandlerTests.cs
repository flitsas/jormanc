using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Commands;

/// <summary>AC2 HU-9784 — PATCH /procedures/{id}/vehicle</summary>
public class CaptureVehicleCommandHandlerTests
{
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
    private readonly IProcedureTypeRepository _typeRepo = Substitute.For<IProcedureTypeRepository>();
    private readonly IVehicleQueryService _vehicleService = Substitute.For<IVehicleQueryService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly CaptureVehicleCommandHandler _sut;
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 30, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly Guid SnapshotId = Guid.NewGuid();

    public CaptureVehicleCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CaptureVehicleCommandHandler(_procedureRepo, _typeRepo, _vehicleService, _clock);
    }

    [Fact]
    public async Task AC2_CapturarPlaca_RetornaVehiculoYWarningsNoBloqueantes()
    {
        var procedure = BuildDraftProcedure();
        var snapshot = new ProcedureTypeSnapshot
        {
            Id = SnapshotId,
            SnapshotJson = """{"vehicleQueryKey":"placa","steps":[]}"""
        };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindSnapshotByIdAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(snapshot);
        _vehicleService.QueryByPlateAsync(TenantId, "AAA123", Arg.Any<CancellationToken>())
            .Returns(new VehicleCaptureResult(
                Found: true,
                RuntPayloadJson: """{"plate":"AAA123"}""",
                SimitPayloadJson: null,
                Warnings: ["Multa SIMIT: $500.000", "Restricción: EMBARGO"],
                Vehicle: new VehicleSummary("AAA123", "VIN123", "TOYOTA", "HILUX", 2022, "BLANCO", "OWNER")));

        var result = await _sut.HandleAsync(
            new CaptureVehicleCommand(ProcedureId, TenantId, UserId, "AAA123", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("draft");
        result.Value.Vehicle.Plate.Should().Be("AAA123");
        result.Value.Warnings.Should().HaveCount(2);
        result.Value.Warnings.Should().Contain("Multa SIMIT: $500.000");

        await _procedureRepo.Received(1).AddVehicleQueryAsync(
            Arg.Is<VehicleQuery>(vq =>
                vq.ProcedureId == ProcedureId &&
                vq.QueryKey == "placa" &&
                vq.QueryValue == "AAA123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_TramiteNoDraft_Retorna409()
    {
        var procedure = BuildDraftProcedure();
        procedure.Status = "submitted";

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);

        var result = await _sut.HandleAsync(
            new CaptureVehicleCommand(ProcedureId, TenantId, UserId, "AAA123", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_INVALID_STATUS");
    }

    private static Procedure BuildDraftProcedure() =>
        new()
        {
            Id = ProcedureId,
            TenantId = TenantId,
            Status = "draft",
            ProcedureTypeId = Guid.NewGuid(),
            ProcedureTypeSnapshotId = SnapshotId
        };
}
