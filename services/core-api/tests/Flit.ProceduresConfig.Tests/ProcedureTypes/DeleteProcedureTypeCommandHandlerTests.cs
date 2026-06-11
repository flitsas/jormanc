using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

/// <summary>AC3 HU-9779 — soft-delete bloqueado con trámites activos</summary>
public class DeleteProcedureTypeCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DeleteProcedureTypeCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureTypeId = Guid.NewGuid();

    public DeleteProcedureTypeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new DeleteProcedureTypeCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC3_TramitesActivos_Retorna409()
    {
        var entity = new ProcedureType { Id = ProcedureTypeId, TenantId = TenantId, DeletedAt = null };
        _repo.FindByIdAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.HasActiveProceduresAsync(ProcedureTypeId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.HandleAsync(new DeleteProcedureTypeCommand(ProcedureTypeId, TenantId, UserId));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PROCEDURE_TYPE_HAS_ACTIVE_PROCEDURES");
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<ProcedureType>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_SinTramitesActivos_SoftDelete()
    {
        var entity = new ProcedureType { Id = ProcedureTypeId, TenantId = TenantId, DeletedAt = null };
        _repo.FindByIdAsync(ProcedureTypeId, TenantId, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.HasActiveProceduresAsync(ProcedureTypeId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.HandleAsync(new DeleteProcedureTypeCommand(ProcedureTypeId, TenantId, UserId));

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).SoftDeleteAsync(entity, UserId, FixedNow, Arg.Any<CancellationToken>());
    }
}
