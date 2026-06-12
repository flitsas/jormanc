using FluentAssertions;
using Flit.Modules.ProceduresConfig.Domain.Models;
using Flit.Modules.ProceduresConfig.Domain.Services;
using Xunit;

namespace Flit.ProceduresConfig.Tests.RuleSets;

/// <summary>AC2 HU-9780 — detección de conflictos show/hide</summary>
public class CoherenceSimulatorTests
{
    private readonly CoherenceSimulator _sut = new();

    private const string Conditions = """{"operator":"AND","nodes":[{"field":"actor.nature","op":"==","value":"juridica"}]}""";

    [Fact]
    public void AC2_ShowVsHide_MismaCondicion_DetectaConflicto()
    {
        var existing = new RuleDefinition(
            Guid.NewGuid(),
            "Mostrar documento",
            Conditions,
            """[{"type":"show","target":"field.documento_natural"}]""");

        var candidate = new RuleDefinition(
            null,
            "Ocultar adjunto",
            Conditions,
            """[{"type":"hide","target":"field.documento_natural"}]""");

        var result = _sut.Simulate([existing], candidate);

        result.IsCoherent.Should().BeFalse();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].Rule1.Should().Be("Mostrar documento");
        result.Conflicts[0].Rule2.Should().Be("Ocultar adjunto");
    }

    [Fact]
    public void AC2_JsonbPropertyOrder_DetectaConflicto()
    {
        var existing = new RuleDefinition(
            Guid.NewGuid(),
            "Mostrar documento",
            """{"nodes":[{"field":"actor.nature","op":"==","value":"juridica"}],"operator":"AND"}""",
            """[{"type":"show","target":"field.documento_natural"}]""");

        var candidate = new RuleDefinition(
            null,
            "Ocultar adjunto",
            Conditions,
            """[{"type":"hide","target":"field.documento_natural"}]""");

        var result = _sut.Simulate([existing], candidate);

        result.IsCoherent.Should().BeFalse();
        result.Conflicts.Should().HaveCount(1);
    }

    [Fact]
    public void AC1_SinConflicto_RetornaCoherente()
    {
        var existing = new RuleDefinition(
            Guid.NewGuid(),
            "Ocultar otro",
            Conditions,
            """[{"type":"hide","target":"field.otro_campo"}]""");

        var candidate = new RuleDefinition(
            null,
            "Ocultar adjunto",
            Conditions,
            """[{"type":"hide","target":"field.documento_natural"}]""");

        var result = _sut.Simulate([existing], candidate);

        result.IsCoherent.Should().BeTrue();
        result.Conflicts.Should().BeEmpty();
    }
}
