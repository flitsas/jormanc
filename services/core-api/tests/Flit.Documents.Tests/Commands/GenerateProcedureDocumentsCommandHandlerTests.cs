using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Flit.Modules.Documents.Infrastructure.Storage;
using Flit.Modules.Documents.Infrastructure.Templates;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

public class GenerateProcedureDocumentsCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentTemplateStorage _templateStorage = Substitute.For<IDocumentTemplateStorage>();
    private readonly IDocumentFileStorage _fileStorage = new LocalDocumentFileStorage();
    private readonly ITemplateResolver _resolver = new TemplateResolver();
    private readonly IDocumentPdfRenderer _pdfRenderer = new QuestPdfDocumentRenderer();
    private readonly ConsolidateDocumentsCommandHandler _consolidateHandler;
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly GenerateProcedureDocumentsCommandHandler _sut;

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProcedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProcedureTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid DocTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid TemplateId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public GenerateProcedureDocumentsCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero));
        _consolidateHandler = new ConsolidateDocumentsCommandHandler(
            _repo, _fileStorage, new PdfSharpMerger(), _clock);
        _sut = new GenerateProcedureDocumentsCommandHandler(
            _repo, _templateStorage, _fileStorage, _resolver, _pdfRenderer, _consolidateHandler, _clock);
    }

    [Fact]
    public async Task AC1_GeneraDocumentosYConsolidaTrasSubmit()
    {
        const string html = "<html><body><p>{{procedure.composite_id}}</p></body></html>";
        const string contentRef = "templates/test/v1.html";
        var submittedAt = new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero);

        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                Id = ProcedureId,
                TenantId = TenantId,
                ProcedureTypeId = ProcedureTypeId,
                CompositeId = "TRASP-99",
                SubmittedAt = submittedAt
            });

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureTypeDocument
                {
                    DocumentTypeId = DocTypeId,
                    IsRequired = true,
                    OrderIndex = 1,
                    DocumentType = new DocumentType
                    {
                        Id = DocTypeId,
                        Name = "Contrato",
                        LoadType = "generacion"
                    }
                }
            ]);

        _repo.GetProcedureActorsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _repo.GetActorDefinitionsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([]);
        _repo.GetLatestVehicleQueryAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns((VehicleQuery?)null);

        _repo.FindProcedureDocumentAsync(ProcedureId, DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns((ProcedureDocument?)null);

        _repo.FindActiveTemplateAsync(DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new DocumentTemplate
            {
                Id = TemplateId,
                DocumentTypeId = DocTypeId,
                Version = 1,
                ContentRef = contentRef
            });

        _templateStorage.ReadTextAsync(contentRef, Arg.Any<CancellationToken>()).Returns(html);

        var storedDocs = new List<ProcedureDocument>();
        _repo.CreateProcedureDocumentAsync(Arg.Do<ProcedureDocument>(d =>
        {
            d.DocumentType = new DocumentType { Name = "Contrato", LoadType = "generacion" };
            storedDocs.Add(d);
        }), Arg.Any<CancellationToken>());

        _repo.GetProcedureDocumentsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(_ => storedDocs);

        _repo.GetMaxConsolidatedVersionAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _sut.HandleAsync(
            new GenerateProcedureDocumentsCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await _repo.Received().CreateProcedureDocumentAsync(
            Arg.Is<ProcedureDocument>(d => d.Status == "ready" && d.Origin == "generated"),
            Arg.Any<CancellationToken>());

        await _repo.Received(1).CreateConsolidatedPackageAsync(
            Arg.Any<ConsolidatedPackage>(),
            Arg.Any<CancellationToken>());
    }
}
