using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.OtOrganisms;

/// <summary>
/// AC1 HU-9798 — POST /api/v1/ot-organisms crea OT con mode=dashboard, quipux_enabled=false.
/// </summary>
public class CreateOtOrganismCommandHandlerTests
{
    private readonly IOtOrganismRepository _repository = Substitute.For<IOtOrganismRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly CreateOtOrganismCommandHandler _sut;

    public CreateOtOrganismCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateOtOrganismCommandHandler(_repository, _clock);
    }

    [Fact]
    public async Task AC1_CrearOT_SlugNuevo_RetornaDtoConDefaults()
    {
        _repository.SlugExistsAsync(TenantId, "secretaria-bogota", Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateOtOrganismCommand(TenantId, UserId, "secretaria-bogota", "Secretaría de Bogotá");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("secretaria-bogota");
        result.Value.Name.Should().Be("Secretaría de Bogotá");
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.Mode.Should().Be("dashboard");
        result.Value.QuipuxEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AC1_CrearOT_PersisteConModeDashboardYQuipuxDisabled()
    {
        _repository.SlugExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateOtOrganismCommand(TenantId, UserId, "ot-medellin", "OT Medellín");

        await _sut.HandleAsync(command);

        await _repository.Received(1).CreateAsync(
            Arg.Is<OtOrganism>(o =>
                o.TenantId == TenantId &&
                o.Slug == "ot-medellin" &&
                o.Mode == "dashboard" &&
                !o.QuipuxEnabled &&
                o.CreatedBy == UserId &&
                o.UpdatedBy == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CrearOT_SlugDuplicado_Retorna409()
    {
        _repository.SlugExistsAsync(TenantId, "secretaria-bogota", Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateOtOrganismCommand(TenantId, UserId, "secretaria-bogota", "Duplicado");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_SLUG_ALREADY_EXISTS");
        await _repository.DidNotReceive().CreateAsync(Arg.Any<OtOrganism>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_CrearOT_NormalizaSlugAMinusculas()
    {
        _repository.SlugExistsAsync(TenantId, "secretaria-bogota", Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateOtOrganismCommand(TenantId, UserId, "Secretaria-Bogota", "OT");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("secretaria-bogota");
    }
}
