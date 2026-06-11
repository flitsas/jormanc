using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Documents.Application.Queries;
using Flit.Modules.Documents.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Queries;

public class GetProcedureDocumentsQueryHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly GetProcedureDocumentsQueryHandler _sut;

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProcedureId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProcedureTypeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DocTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public GetProcedureDocumentsQueryHandlerTests() => _sut = new(_repo);

    [Fact]
    public async Task AC2_RetornaEstadoDocumentalYVersionesConsolidadas()
    {
        _repo.FindProcedureAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new Procedure
            {
                Id = ProcedureId,
                TenantId = TenantId,
                ProcedureTypeId = ProcedureTypeId
            });

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureTypeDocument
                {
                    DocumentTypeId = DocTypeId,
                    IsRequired = true,
                    OrderIndex = 1,
                    DocumentType = new DocumentType { Name = "Contrato", LoadType = "generacion" }
                }
            ]);

        _repo.GetProcedureDocumentsAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentTypeId = DocTypeId,
                    Origin = "generated",
                    Status = "ready",
                    FileRef = "docs/a.pdf",
                    DocumentType = new DocumentType { Name = "Contrato", LoadType = "generacion" }
                }
            ]);

        _repo.GetConsolidatedPackagesAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new ConsolidatedPackage
                {
                    Version = 1,
                    MergedFileRef = "merged/v1.pdf",
                    DownloadFilename = "TRAMITE_x.pdf",
                    DocCount = 1,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]);

        var result = await _sut.HandleAsync(new GetProcedureDocumentsQuery(ProcedureId, TenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProcedureId.Should().Be(ProcedureId);
        result.Value.Documents.Should().HaveCount(1);
        result.Value.Documents[0].Status.Should().Be("ready");
        result.Value.ConsolidatedPackages.Should().HaveCount(1);
        result.Value.ConsolidatedPackages[0].Version.Should().Be(1);
    }
}
