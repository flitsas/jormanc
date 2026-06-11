using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Commands;

/// <summary>AC1-AC3 HU-9785 — POST /procedures/{id}/actors</summary>
public class AddActorCommandHandlerTests
{
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
    private readonly IProcedureTypeRepository _typeRepo = Substitute.For<IProcedureTypeRepository>();
    private readonly IPersonQueryService _personService = Substitute.For<IPersonQueryService>();
    private readonly ILegalEntityQueryService _legalService = Substitute.For<ILegalEntityQueryService>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly AddActorCommandHandler _sut;
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 15, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid VendedorDefId = Guid.NewGuid();
    private static readonly Guid CompradorDefId = Guid.NewGuid();

    public AddActorCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new AddActorCommandHandler(
            _procedureRepo, _typeRepo, _personService, _legalService, _clock);
    }

    [Fact]
    public async Task AC1_ActorNatural_PersisteQueryResultsRunt()
    {
        var procedure = BuildDraftProcedure();
        var actorDef = new ActorDefinition
        {
            Id = VendedorDefId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            Role = "vendedor",
            AllowedNature = "natural"
        };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindActorAsync(TypeId, VendedorDefId, TenantId, Arg.Any<CancellationToken>()).Returns(actorDef);
        _personService.QueryByDocumentAsync(TenantId, "12345678", Arg.Any<CancellationToken>())
            .Returns(new PersonCaptureResult(true, """{"source":"runt","found":true}""", [], "JUAN TEST"));

        var result = await _sut.HandleAsync(new AddActorCommand(
            ProcedureId, TenantId, UserId, VendedorDefId, "natural", "CC", "12345678", null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.ActorId.Should().NotBeEmpty();
        result.Value.Nature.Should().Be("natural");
        result.Value.Warnings.Should().BeEmpty();

        await _procedureRepo.Received(1).AddActorsAsync(
            Arg.Is<IReadOnlyList<ProcedureActor>>(list =>
                list.Count == 1 &&
                list[0].Nature == "natural" &&
                list[0].DocumentNumber == "12345678" &&
                list[0].QueryResults.Contains("runt")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_ActorJuridica_CreaRepresentanteLegalHijo()
    {
        var procedure = BuildDraftProcedure();
        var actorDef = new ActorDefinition
        {
            Id = CompradorDefId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            Role = "comprador",
            AllowedNature = "ambas"
        };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindActorAsync(TypeId, CompradorDefId, TenantId, Arg.Any<CancellationToken>()).Returns(actorDef);
        _legalService.QueryByNitAsync(TenantId, "900123456", Arg.Any<CancellationToken>())
            .Returns(new LegalEntityCaptureResult(
                true, "MOCK SA", "900123456", "Juan Pérez", "11111111", """{"source":"rues"}""", []));

        var result = await _sut.HandleAsync(new AddActorCommand(
            ProcedureId, TenantId, UserId, CompradorDefId, "juridica", null, null, "900123456", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.LegalRepresentativeActorId.Should().NotBeNull();

        await _procedureRepo.Received(1).AddActorsAsync(
            Arg.Is<IReadOnlyList<ProcedureActor>>(list =>
                list.Count == 2 &&
                list.Any(a => a.Nature == "juridica" && a.Nit == "900123456") &&
                list.Any(a => a.Nature == "representante_legal" &&
                              a.ParentActorId == list.First(x => x.Nature == "juridica").Id &&
                              a.DocumentNumber == "11111111")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_CuotaExcede100_Retorna422SinInsertar()
    {
        var procedure = BuildDraftProcedure();
        var actorDef = new ActorDefinition
        {
            Id = CompradorDefId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            Role = "comprador",
            AllowedNature = "natural"
        };

        var existing = new List<ProcedureActor>
        {
            new() { CuotaPct = 25m },
            new() { CuotaPct = 25m },
            new() { CuotaPct = 25m }
        };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindActorAsync(TypeId, CompradorDefId, TenantId, Arg.Any<CancellationToken>()).Returns(actorDef);
        _procedureRepo.GetActorsAsync(ProcedureId, TenantId, CompradorDefId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.HandleAsync(new AddActorCommand(
            ProcedureId, TenantId, UserId, CompradorDefId, "natural", "CC", "99999999", null, 26m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("CUOTA_SUM_EXCEEDS_100");
        result.Error.CuotaCurrentSum.Should().Be(75m);
        result.Error.CuotaProposed.Should().Be(26m);

        await _procedureRepo.DidNotReceive().AddActorsAsync(
            Arg.Any<IReadOnlyList<ProcedureActor>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_CuotaExacta100_AceptaCuartoCopropietario()
    {
        var procedure = BuildDraftProcedure();
        var actorDef = new ActorDefinition
        {
            Id = CompradorDefId,
            ProcedureTypeId = TypeId,
            TenantId = TenantId,
            Role = "comprador",
            AllowedNature = "natural"
        };

        _procedureRepo.FindByIdAsync(ProcedureId, TenantId, Arg.Any<CancellationToken>()).Returns(procedure);
        _typeRepo.FindActorAsync(TypeId, CompradorDefId, TenantId, Arg.Any<CancellationToken>()).Returns(actorDef);
        _procedureRepo.GetActorsAsync(ProcedureId, TenantId, CompradorDefId, Arg.Any<CancellationToken>())
            .Returns(new List<ProcedureActor>
            {
                new() { CuotaPct = 25m },
                new() { CuotaPct = 25m },
                new() { CuotaPct = 25m }
            });
        _personService.QueryByDocumentAsync(TenantId, "88888888", Arg.Any<CancellationToken>())
            .Returns(new PersonCaptureResult(true, "{}", [], "COPROPIETARIO 4"));

        var result = await _sut.HandleAsync(new AddActorCommand(
            ProcedureId, TenantId, UserId, CompradorDefId, "natural", "CC", "88888888", null, 25m));

        result.IsSuccess.Should().BeTrue();
        await _procedureRepo.Received(1).AddActorsAsync(
            Arg.Is<IReadOnlyList<ProcedureActor>>(list => list[0].CuotaPct == 25m),
            Arg.Any<CancellationToken>());
    }

    private static Procedure BuildDraftProcedure() =>
        new()
        {
            Id = ProcedureId,
            TenantId = TenantId,
            Status = "draft",
            ProcedureTypeId = TypeId
        };
}
