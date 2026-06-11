using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Queries;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Models;
using Flit.Modules.ProceduresConfig.Domain.Services;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.RuleSets;

/// <summary>AC3 HU-9780 — simulación dry-run sin INSERT</summary>
public class SimulateCoherenceQueryHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly ICoherenceSimulator _simulator = Substitute.For<ICoherenceSimulator>();
    private readonly SimulateCoherenceQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    public SimulateCoherenceQueryHandlerTests() =>
        _sut = new SimulateCoherenceQueryHandler(_repo, _simulator);

    [Fact]
    public async Task AC3_Simulate_NoPersiste_RetornaIsCoherent()
    {
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new ProcedureType { Id = TypeId, TenantId = TenantId });
        _repo.GetRuleSetsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([new RuleSet { Id = Guid.NewGuid(), Name = "Regla A" }]);
        _simulator.Simulate(Arg.Any<IReadOnlyList<RuleDefinition>>(), Arg.Any<RuleDefinition>())
            .Returns(new CoherenceSimulationResult(true, []));

        var query = new SimulateCoherenceQuery(
            TypeId, TenantId, "Candidata",
            """{"operator":"AND","nodes":[]}""",
            """[{"type":"hide","target":"field.x"}]""");

        var result = await _sut.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCoherent.Should().BeTrue();
        result.Value.Conflicts.Should().BeEmpty();
        await _repo.DidNotReceive().AddRuleSetAndIncrementVersionAsync(
            Arg.Any<RuleSet>(), Arg.Any<ProcedureType>(), Arg.Any<CancellationToken>());
    }
}
