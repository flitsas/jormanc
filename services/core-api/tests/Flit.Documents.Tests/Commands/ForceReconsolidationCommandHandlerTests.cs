using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Procedures;
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

public class ForceReconsolidationCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentFileStorage _fileStorage = new LocalDocumentFileStorage();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProcedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProcedureTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid DocTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public ForceReconsolidationCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task AC4_FuerzaReconsolidacionConNuevaVersion()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = renderer.RenderHtmlToPdf("<html><body><p>Doc</p></body></html>");
        var fileRef = $"procedures/{TenantId}/{ProcedureId}/generated/existing.pdf";

        await using (var stream = new MemoryStream(pdf))
            await _fileStorage.UploadAsync(fileRef, stream, "application/pdf");

        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                Id = ProcedureId,
                TenantId = TenantId,
                ProcedureTypeId = ProcedureTypeId,
                CompositeId = "TRASP-FORCE"
            });

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureTypeDocument
                {
                    DocumentTypeId = DocTypeId,
                    IsRequired = true,
                    OrderIndex = 1,
                    DocumentType = new DocumentType { Name = "Doc", LoadType = "generacion" }
                }
            ]);

        _repo.GetProcedureActorsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetActorDefinitionsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns([]);
        _repo.GetLatestVehicleQueryAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns((VehicleQuery?)null);
        _repo.FindProcedureDocumentAsync(ProcedureId, DocTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new ProcedureDocument
            {
                DocumentTypeId = DocTypeId,
                Status = "ready",
                FileRef = fileRef
            });

        _repo.GetProcedureDocumentsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureDocument
                {
                    DocumentTypeId = DocTypeId,
                    Status = "ready",
                    FileRef = fileRef,
                    DocumentType = new DocumentType { Name = "Doc", LoadType = "generacion" }
                }
            ]);

        _repo.GetMaxConsolidatedVersionAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(1);

        var generateHandler = new GenerateProcedureDocumentsCommandHandler(
            _repo,
            Substitute.For<IDocumentTemplateStorage>(),
            _fileStorage,
            new TemplateResolver(),
            renderer,
            new ConsolidateDocumentsCommandHandler(_repo, _fileStorage, new PdfSharpMerger(), _clock),
            _clock);

        var sut = new ForceReconsolidationCommandHandler(
            generateHandler,
            new ConsolidateDocumentsCommandHandler(_repo, _fileStorage, new PdfSharpMerger(), _clock));

        var result = await sut.HandleAsync(
            new ForceReconsolidationCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(2);
    }
}
