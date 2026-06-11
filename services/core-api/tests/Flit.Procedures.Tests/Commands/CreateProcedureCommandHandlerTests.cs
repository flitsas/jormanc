using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.Modules.Procedures.Application.Commands;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Commands;

/// <summary>AC1 HU-9784 — POST /procedures</summary>
public class CreateProcedureCommandHandlerTests
{
    private readonly IProcedureRepository _procedureRepo = Substitute.For<IProcedureRepository>();
    private readonly IProcedureTypeRepository _typeRepo = Substitute.For<IProcedureTypeRepository>();
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly CreateProcedureCommandHandler _sut;
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();

    public CreateProcedureCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateProcedureCommandHandler(_procedureRepo, _typeRepo, _companyRepo, _clock);
    }

    [Fact]
    public async Task AC1_CrearTramite_RetornaDraftConCompositeIdYSteps()
    {
        var company = new Company { Id = CompanyId, TenantId = TenantId, Name = "Everest Motors" };
        var procedureType = BuildProcedureType();
        var snapshot = new ProcedureTypeSnapshot
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = TypeId,
            Version = 1,
            SnapshotJson = """{"vehicleQueryKey":"placa","steps":[{"id":"00000000-0000-0000-0000-000000000001","orderIndex":1,"name":"Vehículo","stepType":"vehicle","isRequired":true,"sections":[]}]}""",
            CreatedAt = FixedNow
        };

        _companyRepo.FindByIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(company);
        _typeRepo.FindByIdWithDetailsAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _typeRepo.FindSnapshotAsync(TypeId, 1, Arg.Any<CancellationToken>()).Returns(snapshot);
        _procedureRepo.CountByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(1);

        var command = new CreateProcedureCommand(TenantId, UserId, TypeId, CompanyId);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("draft");
        result.Value.CompositeId.Should().StartWith("TRASP-");
        result.Value.ProcedureTypeSnapshotId.Should().Be(snapshot.Id);
        result.Value.Steps.Should().HaveCount(1);
        result.Value.Steps[0].Name.Should().Be("Vehículo");
    }

    [Fact]
    public async Task AC1_CrearTramite_PersisteEntidadDraft()
    {
        var company = new Company { Id = CompanyId, TenantId = TenantId, Name = "Everest" };
        var procedureType = BuildProcedureType();
        var snapshot = new ProcedureTypeSnapshot
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = TypeId,
            Version = 1,
            SnapshotJson = """{"vehicleQueryKey":"placa","steps":[]}""",
            CreatedAt = FixedNow
        };

        _companyRepo.FindByIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(company);
        _typeRepo.FindByIdWithDetailsAsync(TypeId, TenantId, Arg.Any<CancellationToken>()).Returns(procedureType);
        _typeRepo.FindSnapshotAsync(TypeId, 1, Arg.Any<CancellationToken>()).Returns(snapshot);
        _procedureRepo.CountByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(0);

        var command = new CreateProcedureCommand(TenantId, UserId, TypeId, CompanyId);
        await _sut.HandleAsync(command);

        await _procedureRepo.Received(1).CreateAsync(
            Arg.Is<Procedure>(p =>
                p.TenantId == TenantId &&
                p.Status == "draft" &&
                p.ProcedureTypeSnapshotId == snapshot.Id &&
                !string.IsNullOrWhiteSpace(p.CompositeId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_TipoTramiteInexistente_Retorna404()
    {
        _companyRepo.FindByIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = CompanyId, TenantId = TenantId, Name = "Test" });
        _typeRepo.FindByIdWithDetailsAsync(TypeId, TenantId, Arg.Any<CancellationToken>())
            .Returns((ProcedureType?)null);

        var result = await _sut.HandleAsync(
            new CreateProcedureCommand(TenantId, UserId, TypeId, CompanyId));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_NOT_FOUND");
    }

    private static ProcedureType BuildProcedureType() =>
        new()
        {
            Id = TypeId,
            TenantId = TenantId,
            Slug = "traspaso-simple",
            Name = "Traspaso Simple",
            Family = "traspasos",
            Scope = "global",
            VehicleQueryKey = "placa",
            Version = 1,
            IsActive = true,
            Steps =
            [
                new ProcedureStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    ProcedureTypeId = TypeId,
                    OrderIndex = 1,
                    Name = "Vehículo",
                    StepType = "vehicle",
                    IsRequired = true
                }
            ]
        };
}
