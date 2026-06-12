using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Domain.Events;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Commands;

/// <summary>AC1-AC2 HU-9786 — POST /procedures/{id}/submit</summary>
public class SubmitProcedureCommandHandlerTests
{
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
    private readonly IProcedureTypeRepository _typeRepo = Substitute.For<IProcedureTypeRepository>();
    private readonly IProcedureEventPublisher _publisher = Substitute.For<IProcedureEventPublisher>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly SubmitProcedureCommandHandler _sut;
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 30, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid SnapshotId = Guid.NewGuid();
    private static readonly Guid VendedorDefId = Guid.NewGuid();

    private const string SnapshotWithRequiredPlaca = """
        {"steps":[{"id":"s1","orderIndex":1,"name":"Datos del vehículo","stepType":"form","isRequired":true,
        "sections":[{"id":"sec1","stepId":"s1","orderIndex":1,"slug":"veh","name":"Veh","isCollapsible":false,
        "fields":[{"id":"f1","sectionId":"sec1","orderIndex":1,"slug":"placa","name":"Placa","fieldType":"text","isRequired":true,"config":"{}"}]}]}]}
        """;

    public SubmitProcedureCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new SubmitProcedureCommandHandler(_procedureRepo, _typeRepo, _publisher, _clock);
    }

    [Fact]
    public async Task AC1_SubmitValido_PasaASubmittedYPublicaEvento()
    {
        var procedure = BuildDraftProcedure("""{"placa":"ABC123"}""");
        var snapshot = new ProcedureTypeSnapshot { Id = SnapshotId, SnapshotJson = SnapshotWithRequiredPlaca };
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId, Version = 2, Steps = [] };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindSnapshotByIdAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(snapshot);
        _typeRepo.GetActorDefinitionsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ActorDefinition>
            {
                new() { Id = VendedorDefId, Role = "vendedor", IsRequired = true }
            });
        _procedureRepo.GetActorsAsync(ProcedureId, TenantId, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProcedureActor> { new() { ActorDefinitionId = VendedorDefId } });
        _typeRepo.FindByIdWithDetailsAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _typeRepo.FindSnapshotAsync(TypeId, procedureType.Version, Arg.Any<CancellationToken>())
            .Returns((ProcedureTypeSnapshot?)null);

        var result = await _sut.HandleAsync(new SubmitProcedureCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("submitted");
        procedure.Status.Should().Be("submitted");

        await _typeRepo.Received(1).AddSnapshotAsync(Arg.Any<ProcedureTypeSnapshot>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishProcedureSubmittedAsync(
            Arg.Is<ProcedureSubmitted>(e => e.ProcedureId == ProcedureId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_ReutilizaSnapshotExistente_SinInsertarDuplicado()
    {
        var procedure = BuildDraftProcedure("""{"placa":"ABC123"}""");
        var existingSnapshot = new ProcedureTypeSnapshot
        {
            Id = SnapshotId,
            ProcedureTypeId = TypeId,
            Version = 1,
            SnapshotJson = SnapshotWithRequiredPlaca,
        };
        var procedureType = new ProcedureType { Id = TypeId, TenantId = TenantId, Version = 1, Steps = [] };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindSnapshotByIdAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(existingSnapshot);
        _typeRepo.GetActorDefinitionsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ActorDefinition>
            {
                new() { Id = VendedorDefId, Role = "vendedor", IsRequired = true },
            });
        _procedureRepo.GetActorsAsync(ProcedureId, TenantId, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProcedureActor> { new() { ActorDefinitionId = VendedorDefId } });
        _typeRepo.FindByIdWithDetailsAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _typeRepo.FindSnapshotAsync(TypeId, 1, Arg.Any<CancellationToken>()).Returns(existingSnapshot);

        var result = await _sut.HandleAsync(new SubmitProcedureCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        procedure.ProcedureTypeSnapshotId.Should().Be(SnapshotId);
        await _typeRepo.DidNotReceive().AddSnapshotAsync(Arg.Any<ProcedureTypeSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_CampoRequeridoFaltante_Retorna422YPermaneceDraft()
    {
        var procedure = BuildDraftProcedure("{}");
        var snapshot = new ProcedureTypeSnapshot { Id = SnapshotId, SnapshotJson = SnapshotWithRequiredPlaca };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindSnapshotByIdAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await _sut.HandleAsync(new SubmitProcedureCommand(ProcedureId, TenantId, UserId));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("REQUIRED_FIELDS_MISSING");
        result.Error.ValidationErrors.Should().Contain(e => e.FieldSlug == "placa");
        procedure.Status.Should().Be("draft");

        await _publisher.DidNotReceive().PublishProcedureSubmittedAsync(
            Arg.Any<ProcedureSubmitted>(), Arg.Any<CancellationToken>());
    }

    private static Procedure BuildDraftProcedure(string stepData) =>
        new()
        {
            Id = ProcedureId,
            TenantId = TenantId,
            Status = "draft",
            ProcedureTypeId = TypeId,
            ProcedureTypeSnapshotId = SnapshotId,
            StepData = stepData
        };
}
