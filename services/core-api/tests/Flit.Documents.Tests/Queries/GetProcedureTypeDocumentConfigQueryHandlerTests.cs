using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.Queries;
using Flit.Modules.Documents.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Queries;

/// <summary>AC3 HU-9789 — GET /procedure-types/{id}/document-config ordenado por order_index</summary>
public class GetProcedureTypeDocumentConfigQueryHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly GetProcedureTypeDocumentConfigQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProcedureTypeId = Guid.NewGuid();

    public GetProcedureTypeDocumentConfigQueryHandlerTests()
    {
        _sut = new GetProcedureTypeDocumentConfigQueryHandler(_repo);
    }

    [Fact]
    public async Task AC3_ListaDocumentConfig_OrdenadaPorOrderIndex()
    {
        _repo.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(true);

        var associations = new List<ProcedureTypeDocument>
        {
            CreateAssociation("Doc A", orderIndex: 2),
            CreateAssociation("Doc B", orderIndex: 0),
            CreateAssociation("Doc C", orderIndex: 1)
        };

        _repo.GetAssociationsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(associations.OrderBy(a => a.OrderIndex).ToList());

        var result = await _sut.HandleAsync(new GetProcedureTypeDocumentConfigQuery(ProcedureTypeId, TenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(d => d.OrderIndex).Should().ContainInOrder(0, 1, 2);
        result.Value.Select(d => d.DocumentTypeName).Should().ContainInOrder("Doc B", "Doc C", "Doc A");
    }

    [Fact]
    public async Task AC3_TipoTramiteInexistente_Retorna404()
    {
        _repo.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.HandleAsync(new GetProcedureTypeDocumentConfigQuery(ProcedureTypeId, TenantId));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_NOT_FOUND");
    }

    private static ProcedureTypeDocument CreateAssociation(string name, int orderIndex) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = ProcedureTypeId,
            DocumentTypeId = Guid.NewGuid(),
            TenantId = TenantId,
            OrderIndex = orderIndex,
            IsRequired = true,
            CreatedAt = DateTimeOffset.UtcNow,
            DocumentType = new DocumentType
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                Name = name,
                LoadType = "carga"
            }
        };
}
