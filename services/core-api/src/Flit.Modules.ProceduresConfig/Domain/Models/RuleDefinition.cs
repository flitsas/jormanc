namespace Flit.Modules.ProceduresConfig.Domain.Models;

/// <summary>Representación en memoria de una regla para el simulador de coherencia.</summary>
public sealed record RuleDefinition(
    Guid? Id,
    string Name,
    string ConditionsJson,
    string ActionsJson);

public sealed record RuleActionDefinition(string Type, string Target);

public sealed record CoherenceSimulationResult(
    bool IsCoherent,
    IReadOnlyList<RuleConflictItem> Conflicts);

public sealed record RuleConflictItem(string Rule1, string Rule2, string Description);
