using FluentAssertions;
using Flit.Modules.Procedures.Domain.Services;
using Xunit;

namespace Flit.Procedures.Tests.Domain;

public class CuotaValidatorTests
{
    [Fact]
    public void Suma101_Rechaza()
    {
        var result = CuotaValidator.Validate(26m, [25m, 25m, 25m]);
        result.IsValid.Should().BeFalse();
        result.CurrentSum.Should().Be(75m);
        result.ProposedSum.Should().Be(101m);
    }

    [Fact]
    public void Suma100_Acepta()
    {
        var result = CuotaValidator.Validate(25m, [25m, 25m, 25m]);
        result.IsValid.Should().BeTrue();
    }
}
