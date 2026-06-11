using System.Collections.Frozen;
using System.Text;
using FluentAssertions;
using Flit.Modules.Documents.Domain.Models;
using Flit.Modules.Documents.Infrastructure.Templates;
using Xunit;

namespace Flit.Documents.Tests.Services;

/// <summary>AC3 HU-9790 — TemplateResolver sustituye marcadores o [marker:NO_DATA].</summary>
public class TemplateResolverTests
{
    private readonly TemplateResolver _sut = new();

    [Fact]
    public void AC3_ResuelveMarcadoresConDatosEInsertaNoDataAuditables()
    {
        var html = """
            <p>Placa: {{vehicle.plate}}</p>
            <p>NIT comprador: {{actor[comprador].nit}}</p>
            """;

        var context = new TemplateContext
        {
            Vehicle = new VehicleContextData { Plate = "AAA123" },
            Actors = FrozenDictionary<string, ActorContextData>.Empty
        };

        var resolved = _sut.Resolve(html, context);

        resolved.Should().Contain("AAA123");
        resolved.Should().NotContain("{{vehicle.plate}}");
        resolved.Should().Contain("[actor[comprador].nit:NO_DATA]");
        resolved.Should().NotContain("{{actor[comprador].nit}}");
    }

    [Fact]
    public void DetectMarkers_RetornaMarcadoresUnicosOrdenados()
    {
        var html = "{{vehicle.plate}} y {{actor[vendedor].full_name}} y {{vehicle.plate}}";

        var markers = _sut.DetectMarkers(html);

        markers.Should().Equal("actor[vendedor].full_name", "vehicle.plate");
    }
}
