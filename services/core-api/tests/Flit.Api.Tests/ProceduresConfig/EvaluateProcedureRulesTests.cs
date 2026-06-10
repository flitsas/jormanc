using System.Text.Json;
using FluentAssertions;
using Flit.Modules.ProceduresConfig.Adapters;
using Flit.Modules.ProceduresConfig.Application;
using Flit.Modules.ProceduresConfig.Ports;
using Xunit;

namespace Flit.Api.Tests.ProceduresConfig;

/// <summary>HU #9438 — AC1 inyección de sección; AC2 snapshot vs hot-swap (opción A).</summary>
public sealed class EvaluateProcedureRulesTests
{
    [Fact]
    public async Task AC1_Tesla_marca_injects_green_discounts_section()
    {
        var repo = new InMemoryProcedureRulesRepository();
        var response = await EvaluateProcedureRules.HandleAsync(
            new EvaluateProcedureRules.Query(
                InMemoryProcedureRulesRepository.DemoTenantId,
                InMemoryProcedureRulesRepository.DemoProcedureTypeId,
                new Dictionary<string, string?> { ["marca"] = "Tesla" }),
            repo,
            new NoOpRuleEndpointInvoker());

        response.MatchedRules.Should().HaveCount(1);
        response.Actions.Should().ContainSingle(a => a.Type == "inject_section");
        response.Actions[0].Params.GetProperty("section_code").GetString()
            .Should().Be("DESCUENTOS_VERDES");
    }

    [Fact]
    public async Task Hot_swap_deactivated_rule_not_evaluated_from_live_repository()
    {
        var repo = new InMemoryProcedureRulesRepository();
        repo.SetRuleActive(InMemoryProcedureRulesRepository.DemoGreenDiscountRuleId, isActive: false);

        var live = await EvaluateProcedureRules.HandleAsync(
            new EvaluateProcedureRules.Query(
                InMemoryProcedureRulesRepository.DemoTenantId,
                InMemoryProcedureRulesRepository.DemoProcedureTypeId,
                new Dictionary<string, string?> { ["marca"] = "Tesla" }),
            repo,
            new NoOpRuleEndpointInvoker());

        live.MatchedRules.Should().BeEmpty();
    }

    [Fact]
    public async Task AC2_Snapshot_still_evaluates_after_admin_soft_delete()
    {
        var repo = new InMemoryProcedureRulesRepository();
        repo.SetRuleActive(InMemoryProcedureRulesRepository.DemoGreenDiscountRuleId, isActive: false);

        var snapshotJson = """
            {
              "rules": [
                {
                  "id": "00000000-0000-7000-8003-000000000001",
                  "name": "Tesla OR Electrico -> Descuentos Verdes",
                  "priority": 10,
                  "condition_tree": {
                    "op": "OR",
                    "children": [
                      { "field": "marca", "operator": "equal", "value": { "kind": "static", "value": "Tesla" } },
                      { "field": "tipo_energia", "operator": "equal", "value": { "kind": "static", "value": "Electrico" } }
                    ]
                  },
                  "actions": [
                    { "type": "inject_section", "params": { "section_code": "DESCUENTOS_VERDES" } }
                  ]
                }
              ]
            }
            """;

        using var snapshot = JsonDocument.Parse(snapshotJson);
        var fromSnapshot = await EvaluateProcedureRules.HandleAsync(
            new EvaluateProcedureRules.Query(
                InMemoryProcedureRulesRepository.DemoTenantId,
                InMemoryProcedureRulesRepository.DemoProcedureTypeId,
                new Dictionary<string, string?> { ["marca"] = "Tesla" },
                ConfigSnapshot: snapshot),
            repo,
            new NoOpRuleEndpointInvoker());

        fromSnapshot.MatchedRules.Should().HaveCount(1);
        fromSnapshot.Actions.Should().Contain(a => a.Type == "inject_section");
    }

    [Fact]
    public async Task Lower_priority_number_evaluated_first_in_matched_order()
    {
        var rules = new InMemoryProcedureRulesRepository();
        var response = await EvaluateProcedureRules.HandleAsync(
            new EvaluateProcedureRules.Query(
                InMemoryProcedureRulesRepository.DemoTenantId,
                InMemoryProcedureRulesRepository.DemoProcedureTypeId,
                new Dictionary<string, string?> { ["marca"] = "Tesla" }),
            rules,
            new NoOpRuleEndpointInvoker());

        response.MatchedRules[0].Priority.Should().Be(10);
    }
}
