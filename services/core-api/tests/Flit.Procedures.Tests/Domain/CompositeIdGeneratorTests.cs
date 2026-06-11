using FluentAssertions;
using Flit.Modules.Procedures.Domain.Services;
using Xunit;

namespace Flit.Procedures.Tests.Domain;

public class CompositeIdGeneratorTests
{
    [Fact]
    public void GeneraFormatoTraspaso()
    {
        var id = CompositeIdGenerator.Generate("traspasos", "EVE", 2);
        id.Should().StartWith("TRASP-02_EVE-");
    }
}
