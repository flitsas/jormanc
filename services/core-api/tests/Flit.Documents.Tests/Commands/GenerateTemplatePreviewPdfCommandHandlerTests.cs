using System.Collections.Frozen;
using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Domain.Models;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Flit.Modules.Documents.Infrastructure.Templates;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

/// <summary>AC3 HU-9790 — pipeline resolución + PDF QuestPDF de prueba.</summary>
public class GenerateTemplatePreviewPdfCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentTemplateStorage _storage = Substitute.For<IDocumentTemplateStorage>();
    private readonly ITemplateResolver _resolver = new TemplateResolver();
    private readonly IDocumentPdfRenderer _pdfRenderer = new QuestPdfDocumentRenderer();
    private readonly GenerateTemplatePreviewPdfCommandHandler _sut;

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DocTypeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TemplateId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public GenerateTemplatePreviewPdfCommandHandlerTests()
    {
        _sut = new GenerateTemplatePreviewPdfCommandHandler(_repo, _storage, _resolver, _pdfRenderer);
    }

    [Fact]
    public async Task AC3_GeneraPdfValidoDesdePlantillaResuelta()
    {
        const string html = "<html><body><p>Placa: {{vehicle.plate}}</p><p>NIT: {{actor[comprador].nit}}</p></body></html>";
        const string contentRef = "templates/test/template.html";

        _repo.FindDocumentTypeByIdAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new DocumentType { Id = DocTypeId, TenantId = TenantId, LoadType = "generacion" });

        _repo.FindTemplateByIdAsync(TemplateId, DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new DocumentTemplate
            {
                Id = TemplateId,
                DocumentTypeId = DocTypeId,
                TenantId = TenantId,
                ContentRef = contentRef,
                Version = 1,
                Status = "active"
            });

        _storage.ReadTextAsync(contentRef, Arg.Any<CancellationToken>()).Returns(html);

        var context = new TemplateContext
        {
            Vehicle = new VehicleContextData { Plate = "AAA123" },
            Actors = FrozenDictionary<string, ActorContextData>.Empty
        };

        var result = await _sut.HandleAsync(
            new GenerateTemplatePreviewPdfCommand(DocTypeId, TemplateId, TenantId, context));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(result.Value, 0, 4).Should().Be("%PDF");

        var resolved = _resolver.Resolve(html, context);
        resolved.Should().Contain("AAA123");
        resolved.Should().Contain("[actor[comprador].nit:NO_DATA]");
    }
}
