using FluentAssertions;
using Flit.SharedKernel;
using Xunit;

namespace Flit.Api.Tests;

/// <summary>
/// Smoke test del esqueleto base FLIT 2.0. Mantiene el proyecto de pruebas
/// compilando y verde tras el reset. Las features agregan sus propios tests.
/// </summary>
public class SharedKernelSmokeTests
{
    [Fact]
    public void Result_Success_ExposesValue()
    {
        var result = Result<int, string>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Result_Failure_ExposesError()
    {
        var result = Result<int, string>.Failure("boom");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("boom");
    }
}
