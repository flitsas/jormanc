using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

public class UpdateProcedureStepCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly UpdateProcedureStepCommandHandler _sut;

    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateProcedureStepCommandHandlerTests()
    {
        _sut = new UpdateProcedureStepCommandHandler(_repo);
    }

    [Fact]
    public async Task AC1_ActualizaOrderIndex()
    {
        var step = new ProcedureStep
        {
            Id = StepId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            OrderIndex = 3,
            Name = "Paso 3",
            StepType = "form"
        };
        _repo.FindStepAsync(TypeId, StepId, TenantId, Arg.Any<CancellationToken>())
            .Returns(step);

        var command = new UpdateProcedureStepCommand(TypeId, StepId, TenantId, UserId, 1, null);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).UpdateStepOrderAsync(TypeId, StepId, 1, TenantId, Arg.Any<CancellationToken>());
    }
}
