using FluentAssertions;
using Flit.Modules.Procedures.Domain.Services;
using Xunit;

namespace Flit.Procedures.Tests.Domain;

public class StepDataValidatorTests
{
    private const string Snapshot = """
        {"steps":[{"id":"s1","orderIndex":1,"name":"Datos del vehículo","stepType":"form","isRequired":true,
        "sections":[{"id":"sec1","stepId":"s1","orderIndex":1,"slug":"veh","name":"Veh","isCollapsible":false,
        "fields":[{"id":"f1","sectionId":"sec1","orderIndex":1,"slug":"placa","name":"Placa","fieldType":"text","isRequired":true,"config":"{}"}]}]}]}
        """;

    [Fact]
    public void CampoRequeridoFaltante_RetornaError()
    {
        var errors = StepDataValidator.ValidateRequiredFields(Snapshot, "{}");
        errors.Should().HaveCount(1);
        errors[0].FieldSlug.Should().Be("placa");
        errors[0].Step.Should().Be("Datos del vehículo");
        errors[0].Error.Should().Be("REQUIRED_FIELD_MISSING");
    }

    [Fact]
    public void CampoRequeridoPresente_SinErrores()
    {
        var errors = StepDataValidator.ValidateRequiredFields(Snapshot, """{"placa":"ABC123"}""");
        errors.Should().BeEmpty();
    }
}
