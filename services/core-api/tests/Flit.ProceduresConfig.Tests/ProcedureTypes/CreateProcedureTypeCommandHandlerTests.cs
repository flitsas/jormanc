using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

/// <summary>AC1 HU-9779 — POST /procedure-types</summary>
public class CreateProcedureTypeCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateProcedureTypeCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateProcedureTypeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateProcedureTypeCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC1_CrearTipoTramite_RetornaDtoConSlug()
    {
        _repo.SlugExistsAsync(TenantId, "traspaso-simple", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateProcedureTypeCommand(
            TenantId, UserId, "Traspaso Simple", "traspasos", "global", null, "placa");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Traspaso Simple");
        result.Value.Slug.Should().Be("traspaso-simple");
        result.Value.Family.Should().Be("traspasos");
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task AC1_CrearTipoTramite_PersisteEntidad()
    {
        _repo.SlugExistsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateProcedureTypeCommand(
            TenantId, UserId, "Traspaso Simple", "traspasos", "global", null, "placa");

        await _sut.HandleAsync(command);

        await _repo.Received(1).CreateAsync(
            Arg.Is<ProcedureType>(t =>
                t.TenantId == TenantId &&
                t.Name == "Traspaso Simple" &&
                t.VehicleQueryKey == "placa"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_SlugDuplicado_Retorna409()
    {
        _repo.SlugExistsAsync(TenantId, "traspaso-simple", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateProcedureTypeCommand(
            TenantId, UserId, "Traspaso Simple", "traspasos", "global", null, "placa");

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_SLUG_ALREADY_EXISTS");
    }
}
