using System.Collections.Frozen;
using System.Text;
using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Infrastructure.Templates;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

/// <summary>AC1–AC2 HU-9790 — upload plantilla HTML y versionamiento activo único.</summary>
public class CreateDocumentTemplateCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentTemplateStorage _storage = Substitute.For<IDocumentTemplateStorage>();
    private readonly ITemplateResolver _resolver = new TemplateResolver();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateDocumentTemplateCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DocTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public CreateDocumentTemplateCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateDocumentTemplateCommandHandler(_repo, _storage, _resolver, _clock);
    }

    [Fact]
    public async Task AC1_UploadPlantilla_DetectaMarcadoresSubeMinIOYCreaVersionActiva()
    {
        var html = """
            <html><body>
            Vendedor: {{actor[vendedor].full_name}}
            Placa: {{vehicle.plate}}
            </body></html>
            """;

        _repo.FindDocumentTypeByIdAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new DocumentType
            {
                Id = DocTypeId,
                TenantId = TenantId,
                LoadType = "generacion",
                Name = "Contrato"
            });
        _repo.GetMaxTemplateVersionAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(0);

        var command = new CreateDocumentTemplateCommand(
            DocTypeId,
            TenantId,
            UserId,
            new MemoryStream(Encoding.UTF8.GetBytes(html)),
            "Primera versión");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(1);
        result.Value.Status.Should().Be("active");
        result.Value.MarkersDetected.Should().BeEquivalentTo(
            ["actor[vendedor].full_name", "vehicle.plate"]);

        var expectedKey = $"templates/{TenantId}/{DocTypeId}/1.html";
        result.Value.ContentRef.Should().Be(expectedKey);

        await _storage.Received(1).UploadAsync(
            expectedKey,
            Arg.Any<Stream>(),
            "text/html; charset=utf-8",
            Arg.Any<CancellationToken>());

        await _repo.Received(1).CreateTemplateAsync(
            Arg.Is<DocumentTemplate>(t =>
                t.DocumentTypeId == DocTypeId &&
                t.TenantId == TenantId &&
                t.Version == 1 &&
                t.Status == "active" &&
                t.ContentRef == expectedKey),
            Arg.Is<IReadOnlyList<TemplateField>>(fields =>
                fields.Count == 2 &&
                fields.Any(f => f.Marker == "actor[vendedor].full_name" && f.DataSource == "actor") &&
                fields.Any(f => f.Marker == "vehicle.plate" && f.DataSource == "vehicle")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_NuevaVersionDeprecaActivaAnteriorYActivaUnica()
    {
        var html = "<html><body>{{procedure.composite_id}}</body></html>";

        _repo.FindDocumentTypeByIdAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new DocumentType { Id = DocTypeId, TenantId = TenantId, LoadType = "generacion" });
        _repo.GetMaxTemplateVersionAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(1);

        var command = new CreateDocumentTemplateCommand(
            DocTypeId,
            TenantId,
            UserId,
            new MemoryStream(Encoding.UTF8.GetBytes(html)),
            null);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(2);
        result.Value.Status.Should().Be("active");

        await _repo.Received(1).DeprecateActiveTemplatesAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>());
        await _repo.Received(1).CreateTemplateAsync(
            Arg.Is<DocumentTemplate>(t => t.Version == 2 && t.Status == "active"),
            Arg.Any<IReadOnlyList<TemplateField>>(),
            Arg.Any<CancellationToken>());
    }
}
