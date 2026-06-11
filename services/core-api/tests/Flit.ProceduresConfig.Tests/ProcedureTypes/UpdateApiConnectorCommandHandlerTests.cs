using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

public class UpdateApiConnectorCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly UpdateApiConnectorCommandHandler _sut;

    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid ConnId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateApiConnectorCommandHandlerTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _sut = new UpdateApiConnectorCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC3_ActualizaParamBindings()
    {
        const string bindings = """{"placa":"step_1.field_placa"}""";
        var connector = new ApiConnector
        {
            Id = ConnId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            Name = "RUNT",
            ParamBindings = "{}"
        };
        _repo.FindApiConnectorAsync(TypeId, ConnId, TenantId, Arg.Any<CancellationToken>())
            .Returns(connector);

        var command = new UpdateApiConnectorCommand(TypeId, ConnId, TenantId, UserId, bindings);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParamBindings.Should().Be(bindings);
        await _repo.Received(1).UpdateApiConnectorAsync(connector, Arg.Any<CancellationToken>());
    }
}
