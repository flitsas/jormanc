using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.Actors;

public class CreateActorDefinitionCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateActorDefinitionCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    public CreateActorDefinitionCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateActorDefinitionCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC1_CreaCompradorYRepresentanteLegal_ConFk()
    {
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId };
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);

        var compradorId = Guid.NewGuid();
        ActorDefinition? persistedComprador = null;

        _repo.AddActorAsync(Arg.Do<ActorDefinition>(a =>
        {
            if (a.Role == "comprador") persistedComprador = a;
        }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci =>
            {
                if (persistedComprador is not null)
                    persistedComprador.Id = compradorId;
            });

        var compradorCmd = new CreateActorDefinitionCommand(
            TypeId, TenantId, UserId, "comprador", "juridica", 1, 1, true, 0, null);
        var compradorResult = await _sut.HandleAsync(compradorCmd);
        compradorResult.IsSuccess.Should().BeTrue();
        compradorResult.Value.Role.Should().Be("comprador");
        compradorResult.Value.AllowedNature.Should().Be("juridica");

        _repo.FindActorAsync(TypeId, compradorId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new ActorDefinition { Id = compradorId, ProcedureTypeId = TypeId, Role = "comprador" });

        var repCmd = new CreateActorDefinitionCommand(
            TypeId, TenantId, UserId, "representante_legal", "natural", 1, 1, true, 1, compradorId);
        var repResult = await _sut.HandleAsync(repCmd);

        repResult.IsSuccess.Should().BeTrue();
        repResult.Value.Role.Should().Be("representante_legal");
        repResult.Value.LegalRepActorId.Should().Be(compradorId);

        await _repo.Received(2).AddActorAsync(
            Arg.Any<ActorDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TipoNoExiste_RetornaNotFound()
    {
        _repo.FindByIdAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns((ProcedureType?)null);

        var command = new CreateActorDefinitionCommand(
            TypeId, TenantId, UserId, "vendedor", "natural", 1, 1, true, 0, null);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_NOT_FOUND");
    }
}
