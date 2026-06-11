using Flit.Modules.ProceduresConfig.Domain.Models;

namespace Flit.Modules.ProceduresConfig.Domain.Services;

public interface ICoherenceSimulator
{
    CoherenceSimulationResult Simulate(
        IReadOnlyList<RuleDefinition> existingRules,
        RuleDefinition candidateRule);
}
