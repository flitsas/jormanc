using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Models;
using Flit.Modules.ProceduresConfig.Domain.Services;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.RuleSets;

public class CreateRuleSetCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly ICoherenceSimulator _simulator = Substitute.For<ICoherenceSimulator>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateRuleSetCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    private const string Conditions = """{"operator":"AND","nodes":[{"field":"actor.nature","op":"==","value":"juridica"}]}""";
    private const string Actions = """[{"type":"hide","target":"field.documento_natural"}]""";

    public CreateRuleSetCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateRuleSetCommandHandler(_repo, _simulator, _clock);
    }

    [Fact]
    public async Task AC1_ReglaCoherente_PersisteEIncrementaVersion()
    {
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId, Version = 1 };
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _repo.GetRuleSetsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RuleSet>());
        _simulator.Simulate(Arg.Any<IReadOnlyList<RuleDefinition>>(), Arg.Any<RuleDefinition>())
            .Returns(new CoherenceSimulationResult(true, []));

        var command = new CreateRuleSetCommand(TypeId, TenantId, UserId, "Ocultar adjunto", Conditions, Actions);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Ocultar adjunto");

        await _repo.Received(1).AddRuleSetAndIncrementVersionAsync(
            Arg.Is<RuleSet>(r => r.Name == "Ocultar adjunto" && r.Conditions == Conditions),
            procedureType,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_Conflicto_NoPersiste()
    {
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId, Version = 2 };
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _repo.GetRuleSetsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([new RuleSet { Id = Guid.NewGuid(), Name = "Mostrar documento" }]);
        _simulator.Simulate(Arg.Any<IReadOnlyList<RuleDefinition>>(), Arg.Any<RuleDefinition>())
            .Returns(new CoherenceSimulationResult(false,
            [new RuleConflictItem("Mostrar documento", "Ocultar adjunto", "conflicto")]));

        var command = new CreateRuleSetCommand(TypeId, TenantId, UserId, "Ocultar adjunto", Conditions, Actions);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RULE_SET_CONFLICT");
        result.Error.Conflicts.Should().HaveCount(1);
        await _repo.DidNotReceive().AddRuleSetAndIncrementVersionAsync(
            Arg.Any<RuleSet>(), Arg.Any<ProcedureType>(), Arg.Any<CancellationToken>());
    }
}
