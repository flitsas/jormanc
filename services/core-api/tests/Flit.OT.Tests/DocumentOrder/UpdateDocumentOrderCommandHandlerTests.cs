using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.DocumentOrder;

/// <summary>AC1 HU-9799 — UPSERT prelación documental en ot_document_orders.</summary>
public class UpdateDocumentOrderCommandHandlerTests
{
    private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
    private readonly IOtDocumentOrderRepository _orderRepo = Substitute.For<IOtDocumentOrderRepository>();
    private readonly IOtDocumentTypeLookup _lookup = Substitute.For<IOtDocumentTypeLookup>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtId = Guid.NewGuid();
    private static readonly Guid ProcedureTypeId = Guid.NewGuid();

    private readonly UpdateDocumentOrderCommandHandler _sut;

    public UpdateDocumentOrderCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new UpdateDocumentOrderCommandHandler(_organismRepo, _orderRepo, _lookup, _clock);
    }

    [Fact]
    public async Task AC1_ActualizarPrelacion_EjecutaUpsertConJsonb()
    {
        var docIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var reordered = new[] { docIds[2], docIds[0], docIds[1], docIds[3], docIds[4] };

        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId, Slug = "secretaria-bogota", Name = "OT" });
        _lookup.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(true);
        _lookup.GetByIdsAsync(TenantId, reordered, Arg.Any<CancellationToken>())
            .Returns(reordered.ToDictionary(
                id => id,
                id => new DocumentType { Id = id, TenantId = TenantId, Name = $"Doc-{id:N}" }));

        var command = new UpdateDocumentOrderCommand(OtId, ProcedureTypeId, TenantId, UserId, reordered);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Updated.Should().BeTrue();
        result.Value.ProcedureTypeId.Should().Be(ProcedureTypeId);
        result.Value.OrderedDocuments.Should().HaveCount(5);
        result.Value.OrderedDocuments[0].OrderIndex.Should().Be(1);
        result.Value.OrderedDocuments[0].DocumentType.Id.Should().Be(reordered[0]);

        var expectedJson = OtDocumentMapper.SerializeOrderedIds(reordered);
        await _orderRepo.Received(1).UpsertAsync(
            Arg.Is<OtDocumentOrder>(o =>
                o.OtId == OtId &&
                o.ProcedureTypeId == ProcedureTypeId &&
                o.TenantId == TenantId &&
                o.OrderedDocumentTypeIds == expectedJson &&
                o.UpdatedBy == UserId &&
                o.UpdatedAt == FixedNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_OtNoExiste_Retorna404()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns((OtOrganism?)null);

        var command = new UpdateDocumentOrderCommand(
            OtId, ProcedureTypeId, TenantId, UserId, [Guid.NewGuid()]);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_NOT_FOUND");
        await _orderRepo.DidNotReceive().UpsertAsync(Arg.Any<OtDocumentOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_TipoTramiteNoExiste_Retorna404()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _lookup.ProcedureTypeExistsAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateDocumentOrderCommand(
            OtId, ProcedureTypeId, TenantId, UserId, [Guid.NewGuid()]);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_PROCEDURE_TYPE_NOT_FOUND");
    }
}
