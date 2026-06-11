using System.Text.Json;
using Flit.Modules.ProceduresConfig.Domain.Models;

namespace Flit.Modules.ProceduresConfig.Domain.Services;

/// <summary>
/// Detecta conflictos entre reglas de UI (show/hide sobre el mismo target con mismas condiciones).
/// HU-9780 — AC2.
/// </summary>
public sealed class CoherenceSimulator : ICoherenceSimulator
{
    private static readonly HashSet<string> OpposingPairs =
        new(StringComparer.OrdinalIgnoreCase) { "show", "hide" };

    public CoherenceSimulationResult Simulate(
        IReadOnlyList<RuleDefinition> existingRules,
        RuleDefinition candidateRule)
    {
        var conflicts = new List<RuleConflictItem>();
        var candidateFingerprint = NormalizeConditions(candidateRule.ConditionsJson);
        var candidateActions = ParseActions(candidateRule.ActionsJson);

        foreach (var existing in existingRules)
        {
            if (existing.Id.HasValue && candidateRule.Id.HasValue && existing.Id == candidateRule.Id)
                continue;

            if (NormalizeConditions(existing.ConditionsJson) != candidateFingerprint)
                continue;

            var existingActions = ParseActions(existing.ActionsJson);
            foreach (var ca in candidateActions)
            {
                foreach (var ea in existingActions)
                {
                    if (!string.Equals(ca.Target, ea.Target, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (AreOpposing(ca.Type, ea.Type))
                    {
                        conflicts.Add(new RuleConflictItem(
                            Rule1: existing.Name,
                            Rule2: candidateRule.Name,
                            Description:
                            $"Regla '{existing.Name}' {ea.Type} {ea.Target}; Regla '{candidateRule.Name}' {ca.Type} {ca.Target} con igual condición"));
                    }
                }
            }
        }

        return new CoherenceSimulationResult(conflicts.Count == 0, conflicts);
    }

    private static bool AreOpposing(string typeA, string typeB) =>
        OpposingPairs.Contains(typeA) && OpposingPairs.Contains(typeB) &&
        !string.Equals(typeA, typeB, StringComparison.OrdinalIgnoreCase);

    internal static string NormalizeConditions(string conditionsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(conditionsJson);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            return conditionsJson;
        }
    }

    internal static List<RuleActionDefinition> ParseActions(string actionsJson)
    {
        var result = new List<RuleActionDefinition>();
        try
        {
            using var doc = JsonDocument.Parse(actionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                var target = item.TryGetProperty("target", out var tg) ? tg.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(target))
                    result.Add(new RuleActionDefinition(type.ToLowerInvariant(), target));
            }
        }
        catch
        {
            // vacío — validación JSON ocurre en el handler
        }

        return result;
    }
}
