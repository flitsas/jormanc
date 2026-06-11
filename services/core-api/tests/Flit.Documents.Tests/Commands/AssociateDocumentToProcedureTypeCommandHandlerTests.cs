using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

/// <summary>AC1/AC2 HU-9789 — POST /procedure-types/{id}/document-config</summary>
public class AssociateDocumentToProcedureTypeCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly AssociateDocumentToProcedureTypeCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureTypeId = Guid.NewGuid();
    private static readonly Guid DocumentTypeId = Guid.NewGuid();

    public AssociateDocumentToProcedureTypeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new AssociateDocumentToProcedureTypeCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC1_AsociarDocumento_RetornaConfig201()
    {
        var docType = new DocumentType
        {
            Id = DocumentTypeId,
            TenantId = TenantId,
            Name = "Tarjeta propiedad",
            LoadType = "carga"
        };

        _repo.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.FindDocumentTypeByIdAsync(DocumentTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(docType);
        _repo.AssociationExistsAsync(ProcedureTypeId, DocumentTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new AssociateDocumentToProcedureTypeCommand(
            ProcedureTypeId, TenantId, UserId, DocumentTypeId, true, 1, null, false);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProcedureTypeId.Should().Be(ProcedureTypeId);
        result.Value.DocumentTypeId.Should().Be(DocumentTypeId);
        result.Value.DocumentTypeName.Should().Be("Tarjeta propiedad");
        result.Value.OrderIndex.Should().Be(1);
    }

    [Fact]
    public async Task AC2_AsociacionDuplicada_Retorna409()
    {
        var docType = new DocumentType { Id = DocumentTypeId, TenantId = TenantId, Name = "Doc", LoadType = "carga" };

        _repo.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.FindDocumentTypeByIdAsync(DocumentTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(docType);
        _repo.AssociationExistsAsync(ProcedureTypeId, DocumentTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new AssociateDocumentToProcedureTypeCommand(
            ProcedureTypeId, TenantId, UserId, DocumentTypeId, true, 0, null, false);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DOCUMENT_ALREADY_ASSOCIATED");
        await _repo.DidNotReceive().CreateAssociationAsync(
            Arg.Any<ProcedureTypeDocument>(), Arg.Any<CancellationToken>());
    }
}
