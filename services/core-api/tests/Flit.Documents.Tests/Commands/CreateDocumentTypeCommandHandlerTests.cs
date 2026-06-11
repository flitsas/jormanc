using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.Commands;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Documents.Tests.Commands;

/// <summary>AC1 HU-9789 — POST /document-types y POST /procedure-types/{id}/document-config</summary>
public class CreateDocumentTypeCommandHandlerTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateDocumentTypeCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateDocumentTypeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateDocumentTypeCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC1_CrearTipoDocumento_RetornaDto201()
    {
        var command = new CreateDocumentTypeCommand(
            TenantId, UserId, "Cédula vendedor", "carga", null, 10, true);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Cédula vendedor");
        result.Value.LoadType.Should().Be("carga");
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.MaxSizeMb.Should().Be(10);
    }

    [Fact]
    public async Task AC1_CrearTipoDocumento_PersisteEntidad()
    {
        var command = new CreateDocumentTypeCommand(
            TenantId, UserId, "Contrato compraventa", "generacion", """["pdf"]""", 5, false);

        await _sut.HandleAsync(command);

        await _repo.Received(1).CreateDocumentTypeAsync(
            Arg.Is<DocumentType>(d =>
                d.TenantId == TenantId &&
                d.Name == "Contrato compraventa" &&
                d.LoadType == "generacion" &&
                d.IsReusable == false),
            Arg.Any<CancellationToken>());
    }
}
