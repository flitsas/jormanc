using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.Labels;

/// <summary>AC3 HU-9799 — Crear etiqueta con slug único por OT.</summary>
public class CreateOtLabelCommandHandlerTests
{
    private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
    private readonly IOtDocumentLabelRepository _labelRepo = Substitute.For<IOtDocumentLabelRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 30, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtId = Guid.NewGuid();

    private readonly CreateOtLabelCommandHandler _sut;

    public CreateOtLabelCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateOtLabelCommandHandler(_organismRepo, _labelRepo, _clock);
    }

    [Fact]
    public async Task AC3_CrearEtiqueta_InsertaConIsActiveTrue()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId, Slug = "secretaria-bogota" });
        _labelRepo.SlugExistsAsync(OtId, "paz_y_salvo", TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateOtLabelCommand(OtId, TenantId, UserId, "paz_y_salvo", "Paz y Salvo Municipal");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("paz_y_salvo");
        result.Value.DisplayName.Should().Be("Paz y Salvo Municipal");
        result.Value.IsActive.Should().BeTrue();
        result.Value.OtId.Should().Be(OtId);

        await _labelRepo.Received(1).CreateAsync(
            Arg.Is<OtDocumentLabel>(l =>
                l.OtId == OtId &&
                l.TenantId == TenantId &&
                l.Slug == "paz_y_salvo" &&
                l.DisplayName == "Paz y Salvo Municipal" &&
                l.IsActive &&
                l.CreatedBy == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_SlugDuplicado_Retorna409()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.SlugExistsAsync(OtId, "paz_y_salvo", TenantId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateOtLabelCommand(OtId, TenantId, UserId, "paz_y_salvo", "Duplicado");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_LABEL_SLUG_ALREADY_EXISTS");
        await _labelRepo.DidNotReceive().CreateAsync(Arg.Any<OtDocumentLabel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_NormalizaSlugAMinusculas()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.SlugExistsAsync(OtId, "paz_y_salvo", TenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateOtLabelCommand(OtId, TenantId, UserId, "Paz_Y_Salvo", "Paz y Salvo");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("paz_y_salvo");
    }
}
