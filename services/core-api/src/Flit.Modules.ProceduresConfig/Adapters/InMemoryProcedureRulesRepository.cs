using System.Text.Json;
using Flit.Modules.ProceduresConfig.Domain;
using Flit.Modules.ProceduresConfig.Ports;

namespace Flit.Modules.ProceduresConfig.Adapters;

/// <summary>Reglas de demo para DEV sin PostgreSQL (AC1 Tesla OR Eléctrico).</summary>
public sealed class InMemoryProcedureRulesRepository : IProcedureRulesRepository
{
    public static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-7000-8001-000000000010");
    public static readonly Guid DemoProcedureTypeId = Guid.Parse("00000000-0000-7000-8002-000000000001");
    public static readonly Guid DemoGreenDiscountRuleId = Guid.Parse("00000000-0000-7000-8003-000000000001");

    private static readonly JsonElement TeslaOrElectricCondition = JsonRuleElements.Parse("""
        {
          "op": "OR",
          "children": [
            { "field": "marca", "operator": "equal", "value": { "kind": "static", "value": "Tesla" } },
            { "field": "tipo_energia", "operator": "equal", "value": { "kind": "static", "value": "Electrico" } }
          ]
        }
        """);

    private static readonly JsonElement InjectGreenSectionActions = JsonRuleElements.Parse("""
        [
          {
            "type": "inject_section",
            "params": { "section_code": "DESCUENTOS_VERDES", "section_label": "Descuentos Verdes" }
          }
        ]
        """);

    private readonly List<ProcedureRuleRecord> _rules =
    [
        new(
            DemoGreenDiscountRuleId,
            "Tesla OR Electrico -> Descuentos Verdes",
            Priority: 10,
            TeslaOrElectricCondition,
            InjectGreenSectionActions),
    ];

    public Task<IReadOnlyList<ProcedureRuleRecord>> ListActiveForEvaluationAsync(
        Guid tenantId,
        Guid procedureTypeId,
        CancellationToken ct = default)
    {
        if (tenantId != DemoTenantId || procedureTypeId != DemoProcedureTypeId)
        {
            return Task.FromResult<IReadOnlyList<ProcedureRuleRecord>>([]);
        }

        return Task.FromResult<IReadOnlyList<ProcedureRuleRecord>>(_rules
            .OrderBy(r => r.Priority)
            .ToList());
    }

    /// <summary>Simula desactivación hot-swap (is_active=false) sin afectar snapshots.</summary>
    public void SetRuleActive(Guid ruleId, bool isActive)
    {
        if (!isActive)
        {
            _rules.RemoveAll(r => r.Id == ruleId);
        }
    }
}
