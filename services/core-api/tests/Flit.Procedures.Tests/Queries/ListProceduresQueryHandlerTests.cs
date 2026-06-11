using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Modules.Procedures.Application.Queries;
using Flit.Modules.Procedures.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Procedures.Tests.Queries;

/// <summary>AC3 HU-9784 — GET /procedures</summary>
public class ListProceduresQueryHandlerTests
{
    private readonly IProcedureRepository _repo = Substitute.For<IProcedureRepository>();
    private readonly ListProceduresQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public ListProceduresQueryHandlerTests()
    {
        _sut = new ListProceduresQueryHandler(_repo);
    }

    [Fact]
    public async Task AC3_ListarTramites_FiltraPorTenantYPagina()
    {
        var items = new List<Procedure>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                CompositeId = "TRASP-01_EVE-1200",
                Status = "draft",
                ProcedureTypeId = Guid.NewGuid(),
                CompanyId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _repo.ListAsync(
                Arg.Is<ProcedureListFilter>(f =>
                    f.TenantId == TenantId && f.Page == 1 && f.PageSize == 20),
                Arg.Any<CancellationToken>())
            .Returns((items, 1));

        var result = await _sut.HandleAsync(new ListProceduresQuery(TenantId));

        result.Data.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.Data[0].CompositeId.Should().Be("TRASP-01_EVE-1200");

        await _repo.Received(1).ListAsync(
            Arg.Is<ProcedureListFilter>(f => f.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ListarTramites_FiltraPorStatus()
    {
        _repo.ListAsync(Arg.Any<ProcedureListFilter>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Procedure>(), 0));

        await _sut.HandleAsync(new ListProceduresQuery(TenantId, Status: "draft"));

        await _repo.Received(1).ListAsync(
            Arg.Is<ProcedureListFilter>(f => f.TenantId == TenantId && f.Status == "draft"),
            Arg.Any<CancellationToken>());
    }
}
