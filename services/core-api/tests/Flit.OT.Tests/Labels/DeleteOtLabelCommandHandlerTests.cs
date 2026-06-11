using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.Labels;

/// <summary>AC2 HU-9799 — Delete etiqueta con validación de impacto y confirm.</summary>
public class DeleteOtLabelCommandHandlerTests
{
    private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
    private readonly IOtDocumentLabelRepository _labelRepo = Substitute.For<IOtDocumentLabelRepository>();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtId = Guid.NewGuid();
    private static readonly Guid LabelId = Guid.NewGuid();

    private readonly DeleteOtLabelCommandHandler _sut;

    public DeleteOtLabelCommandHandlerTests()
    {
        _sut = new DeleteOtLabelCommandHandler(_organismRepo, _labelRepo);
    }

    [Fact]
    public async Task AC2_DeleteSinConfirm_ConAdjuntos_Retorna409ConImpactCount()
    {
        var label = new OtDocumentLabel
        {
            Id = LabelId,
            OtId = OtId,
            TenantId = TenantId,
            Slug = "licencia_transito",
            DisplayName = "Licencia de Tránsito"
        };

        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.FindByIdAsync(LabelId, OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(label);
        _labelRepo.CountAttachmentImpactAsync(OtId, "licencia_transito", TenantId, Arg.Any<CancellationToken>())
            .Returns(15);

        var command = new DeleteOtLabelCommand(OtId, LabelId, TenantId, UserId, Confirm: false);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("LABEL_IN_USE");
        result.Error.ImpactCount.Should().Be(15);
        await _labelRepo.DidNotReceive().DeleteAsync(Arg.Any<OtDocumentLabel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_DeleteConConfirm_EliminaYRetornaAffectedAttachments()
    {
        var label = new OtDocumentLabel
        {
            Id = LabelId,
            OtId = OtId,
            TenantId = TenantId,
            Slug = "licencia_transito",
            DisplayName = "Licencia de Tránsito"
        };

        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.FindByIdAsync(LabelId, OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(label);
        _labelRepo.CountAttachmentImpactAsync(OtId, "licencia_transito", TenantId, Arg.Any<CancellationToken>())
            .Returns(15);

        var command = new DeleteOtLabelCommand(OtId, LabelId, TenantId, UserId, Confirm: true);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deleted.Should().BeTrue();
        result.Value.AffectedAttachments.Should().Be(15);
        await _labelRepo.Received(1).DeleteAsync(label, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_DeleteSinAdjuntos_EliminaSinRequerirConfirm()
    {
        var label = new OtDocumentLabel
        {
            Id = LabelId,
            OtId = OtId,
            TenantId = TenantId,
            Slug = "etiqueta_libre",
            DisplayName = "Etiqueta libre"
        };

        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.FindByIdAsync(LabelId, OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(label);
        _labelRepo.CountAttachmentImpactAsync(OtId, "etiqueta_libre", TenantId, Arg.Any<CancellationToken>())
            .Returns(0);

        var command = new DeleteOtLabelCommand(OtId, LabelId, TenantId, UserId, Confirm: false);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deleted.Should().BeTrue();
        result.Value.AffectedAttachments.Should().Be(0);
        await _labelRepo.Received(1).DeleteAsync(label, Arg.Any<CancellationToken>());
    }
}
