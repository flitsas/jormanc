using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Commands;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.OtOrganisms;

/// <summary>
/// AC2 HU-9798 — PATCH /api/v1/ot-organisms/{id}/mode alterna dashboard ↔ qx.
/// </summary>
public class UpdateOtModeCommandHandlerTests
{
    private readonly IOtOrganismRepository _repository = Substitute.For<IOtOrganismRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 30, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtId = Guid.NewGuid();

    private readonly UpdateOtModeCommandHandler _sut;

    public UpdateOtModeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new UpdateOtModeCommandHandler(_repository, _clock);
    }

    [Fact]
    public async Task AC2_CambiarAQx_ActivaQuipuxEnabled()
    {
        var organism = BuildOrganism(mode: "dashboard", quipuxEnabled: false);
        _repository.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>()).Returns(organism);

        var result = await _sut.HandleAsync(new UpdateOtModeCommand(OtId, TenantId, UserId, "qx"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be("qx");
        result.Value.QuipuxEnabled.Should().BeTrue();
        organism.Mode.Should().Be("qx");
        organism.QuipuxEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task AC2_RevertirADashboard_DesactivaQuipuxEnabled()
    {
        var organism = BuildOrganism(mode: "qx", quipuxEnabled: true);
        _repository.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>()).Returns(organism);

        var result = await _sut.HandleAsync(new UpdateOtModeCommand(OtId, TenantId, UserId, "dashboard"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Mode.Should().Be("dashboard");
        result.Value.QuipuxEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AC2_ModoInvalido_RetornaError()
    {
        var organism = BuildOrganism();
        _repository.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>()).Returns(organism);

        var result = await _sut.HandleAsync(new UpdateOtModeCommand(OtId, TenantId, UserId, "invalid"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_INVALID_MODE");
    }

    [Fact]
    public async Task AC2_OTNoExiste_RetornaNotFound()
    {
        _repository.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns((OtOrganism?)null);

        var result = await _sut.HandleAsync(new UpdateOtModeCommand(OtId, TenantId, UserId, "qx"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("OT_NOT_FOUND");
    }

    private static OtOrganism BuildOrganism(string mode = "dashboard", bool quipuxEnabled = false) => new()
    {
        Id = OtId,
        TenantId = TenantId,
        Slug = "secretaria-bogota",
        Name = "Secretaría de Bogotá",
        Mode = mode,
        QuipuxEnabled = quipuxEnabled,
        CreatedAt = FixedNow.AddDays(-1),
        CreatedBy = UserId,
        UpdatedAt = FixedNow.AddDays(-1),
        UpdatedBy = UserId
    };
}
