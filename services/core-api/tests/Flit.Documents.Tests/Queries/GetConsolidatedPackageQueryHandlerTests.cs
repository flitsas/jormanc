using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Documents.Application.Queries;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Infrastructure.Pdf;
using Flit.Modules.Documents.Infrastructure.Storage;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Queries;

public class GetConsolidatedPackageQueryHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IDocumentFileStorage _storage = new LocalDocumentFileStorage();
    private readonly GetConsolidatedPackageQueryHandler _sut;

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProcedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public GetConsolidatedPackageQueryHandlerTests() => _sut = new(_repo, _storage);

    [Fact]
    public async Task AC3_DescargaUltimaVersionConsolidada()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = renderer.RenderHtmlToPdf("<html><body><p>Consolidado</p></body></html>");
        const string mergedRef = "procedures/merged/v1.pdf";
        const string filename = "TRAMITE_TEST.pdf";

        await using (var stream = new MemoryStream(pdf))
            await _storage.UploadAsync(mergedRef, stream, "application/pdf");

        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure { Id = ProcedureId, TenantId = TenantId });

        _repo.GetLatestConsolidatedPackageAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new ConsolidatedPackage
            {
                Version = 2,
                MergedFileRef = mergedRef,
                DownloadFilename = filename
            });

        var result = await _sut.HandleAsync(new GetConsolidatedPackageQuery(ProcedureId, TenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().NotBeEmpty();
        result.Value.Filename.Should().Be(filename);
        System.Text.Encoding.ASCII.GetString(result.Value.Content, 0, 4).Should().Be("%PDF");
    }
}
