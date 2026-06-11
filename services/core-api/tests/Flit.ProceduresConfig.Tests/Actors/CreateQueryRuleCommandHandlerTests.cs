using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.Actors;

public class CreateQueryRuleCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateQueryRuleCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 30, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private const string Verifications =
        """[{"type":"datos_persona","is_active":true,"order":1},{"type":"simit","is_active":true,"order":2}]""";

    public CreateQueryRuleCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateQueryRuleCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC2_PersisteVerificationsJsonB_OrdenadasYBlocking()
    {
        _repo.FindActorAsync(TypeId, ActorId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new ActorDefinition { Id = ActorId, ProcedureTypeId = TypeId, Role = "vendedor" });

        var command = new CreateQueryRuleCommand(
            TypeId, ActorId, TenantId, UserId, "persona_natural", "document_number", true, Verifications);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.SubjectType.Should().Be("persona_natural");
        result.Value.EntryKey.Should().Be("document_number");
        result.Value.IsBlocking.Should().BeTrue();
        result.Value.Verifications.Should().Be(Verifications);

        await _repo.Received(1).AddQueryRuleAsync(
            Arg.Is<QueryRule>(q =>
                q.IsBlocking &&
                q.Verifications == Verifications &&
                q.SubjectType == "persona_natural"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActorNoExiste_RetornaNotFound()
    {
        _repo.FindActorAsync(TypeId, ActorId, TenantId, Arg.Any<CancellationToken>())
            .Returns((ActorDefinition?)null);

        var command = new CreateQueryRuleCommand(
            TypeId, ActorId, TenantId, UserId, "persona_natural", "document_number", true, "[]");
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ACTOR_NOT_FOUND");
    }
}
