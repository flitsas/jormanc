using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Flit.Modules.Documents.Infrastructure.Storage;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

public class ConsolidateDocumentsCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentFileStorage _storage = new LocalDocumentFileStorage();
    private readonly IPdfMerger _merger = new PdfSharpMerger();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProcedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProcedureTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid DocTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public ConsolidateDocumentsCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 11, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task AC1_ConsolidaDocumentosYVersionaPaquete()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = renderer.RenderHtmlToPdf("<html><body><p>Contrato</p></body></html>");
        var fileRef = $"procedures/{TenantId}/{ProcedureId}/generated/doc.pdf";

        await using (var stream = new MemoryStream(pdf))
            await _storage.UploadAsync(fileRef, stream, "application/pdf");

        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                Id = ProcedureId,
                TenantId = TenantId,
                ProcedureTypeId = ProcedureTypeId,
                CompositeId = "TRASP-01_EVE-100"
            });

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureTypeDocument
                {
                    DocumentTypeId = DocTypeId,
                    OrderIndex = 1,
                    IsRequired = true,
                    DocumentType = new DocumentType { Name = "Contrato", LoadType = "generacion" }
                }
            ]);

        _repo.GetProcedureDocumentsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureDocument
                {
                    DocumentTypeId = DocTypeId,
                    Status = "ready",
                    FileRef = fileRef,
                    DocumentType = new DocumentType { Name = "Contrato", LoadType = "generacion" }
                }
            ]);

        _repo.GetMaxConsolidatedVersionAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = new ConsolidateDocumentsCommandHandler(_repo, _storage, _merger, _clock);

        var result = await sut.HandleAsync(
            new ConsolidateDocumentsCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(1);
        result.Value.DocCount.Should().Be(1);
        result.Value.DownloadFilename.Should().StartWith("TRAMITE_");

        await _repo.Received(1).CreateConsolidatedPackageAsync(
            Arg.Is<ConsolidatedPackage>(p => p.Version == 1 && p.DocCount == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC5_VersionamientoIncrementaVersion()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = renderer.RenderHtmlToPdf("<html><body><p>Doc</p></body></html>");
        var fileRef = $"procedures/{TenantId}/{ProcedureId}/generated/doc2.pdf";

        await using (var stream = new MemoryStream(pdf))
            await _storage.UploadAsync(fileRef, stream, "application/pdf");

        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                Id = ProcedureId,
                TenantId = TenantId,
                ProcedureTypeId = ProcedureTypeId,
                CompositeId = "TRASP-02"
            });

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureTypeDocument
                {
                    DocumentTypeId = DocTypeId,
                    OrderIndex = 1,
                    IsRequired = true,
                    DocumentType = new DocumentType { Name = "Doc", LoadType = "generacion" }
                }
            ]);

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
            .Returns(2);

        var sut = new ConsolidateDocumentsCommandHandler(_repo, _storage, _merger, _clock);
        var result = await sut.HandleAsync(
            new ConsolidateDocumentsCommand(ProcedureId, TenantId, UserId, Force: true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(3);
    }
}
