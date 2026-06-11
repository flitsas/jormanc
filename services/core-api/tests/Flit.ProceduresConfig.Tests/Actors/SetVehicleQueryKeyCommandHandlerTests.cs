using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.Actors;

public class SetVehicleQueryKeyCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly SetVehicleQueryKeyCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 17, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    public SetVehicleQueryKeyCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new SetVehicleQueryKeyCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC3_ActualizaVehicleQueryKeyEIncrementaVersion()
    {
        var procedureType = new ProcedureType
        {
            Id = TypeId,
            TenantId = TenantId,
            VehicleQueryKey = "placa",
            Version = 3
        };
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);

        _repo.UpdateVehicleQueryKeyAsync(
                procedureType, "vin", UserId, FixedNow, Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                procedureType.VehicleQueryKey = "vin";
                procedureType.Version = 4;
                return Task.CompletedTask;
            });

        var command = new SetVehicleQueryKeyCommand(TypeId, TenantId, UserId, "vin");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.VehicleQueryKey.Should().Be("vin");
        result.Value.Version.Should().Be(4);

        await _repo.Received(1).UpdateVehicleQueryKeyAsync(
            procedureType, "vin", UserId, FixedNow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryKeyInvalida_RetornaError()
    {
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId, Version = 1 };
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);

        var command = new SetVehicleQueryKeyCommand(TypeId, TenantId, UserId, "invalid");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_INVALID_VEHICLE_QUERY_KEY");
    }
}
