using System.Text.Json;
using FluentAssertions;
using Flit.Modules.ProceduresConfig.Domain;
using Xunit;

namespace Flit.Api.Tests.ProceduresConfig;

public sealed class RuleConditionEvaluatorTests
{
    [Fact]
    public void Or_group_matches_when_any_child_matches()
    {
        var tree = JsonSerializer.SerializeToElement(new
        {
            op = "OR",
            children = new object[]
            {
                new { field = "marca", @operator = "equal", value = new { kind = "static", value = "Tesla" } },
                new { field = "marca", @operator = "equal", value = new { kind = "static", value = "BYD" } },
            },
        });

        var fields = new Dictionary<string, string?> { ["marca"] = "Tesla" };

        RuleConditionEvaluator.Evaluate(tree, fields).Should().BeTrue();
    }

    [Fact]
    public void And_group_requires_all_children()
    {
        var tree = JsonSerializer.SerializeToElement(new
        {
            op = "AND",
            children = new object[]
            {
                new { field = "marca", @operator = "equal", value = new { kind = "static", value = "Tesla" } },
                new { field = "tipo_energia", @operator = "equal", value = new { kind = "static", value = "Electrico" } },
            },
        });

        var fields = new Dictionary<string, string?> { ["marca"] = "Tesla", ["tipo_energia"] = "Gasolina" };

        RuleConditionEvaluator.Evaluate(tree, fields).Should().BeFalse();
    }

    [Fact]
    public void Dynamic_field_value_resolves_from_captured_data()
    {
        var tree = JsonSerializer.SerializeToElement(new
        {
            field = "placa",
            @operator = "equal",
            value = new { kind = "field", field = "placa_confirmada" },
        });

        var fields = new Dictionary<string, string?>
        {
            ["placa"] = "ABC123",
            ["placa_confirmada"] = "ABC123",
        };

        RuleConditionEvaluator.Evaluate(tree, fields).Should().BeTrue();
    }
}
