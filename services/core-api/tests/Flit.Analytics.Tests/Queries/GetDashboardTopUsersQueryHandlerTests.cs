using FluentAssertions;
using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Analytics.Tests.Queries;

/// <summary>HU-9797 — GET /api/v1/dashboard/top-users</summary>
public class GetDashboardTopUsersQueryHandlerTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();
    private readonly GetDashboardTopUsersQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public GetDashboardTopUsersQueryHandlerTests()
    {
        _sut = new GetDashboardTopUsersQueryHandler(_repository);
    }

    [Fact]
    public async Task AC1_TopUsers_RetornaTop5ConPorcentaje()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        _repository.GetTopUsersAsync(Arg.Any<DashboardTopUsersFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new TopUserRow(user1, "María Pérez", 40),
                new TopUserRow(user2, "Carlos López", 10)
            ]);

        var result = await _sut.HandleAsync(new GetDashboardTopUsersQuery(TenantId, From, To, null));

        result.Data.Should().HaveCount(2);
        result.Data[0].FullName.Should().Be("María Pérez");
        result.Data[0].Count.Should().Be(40);
        result.Data[0].PctOfTotal.Should().Be(80m);
        result.Data[1].PctOfTotal.Should().Be(20m);
    }

    [Fact]
    public async Task AC1_TopUsers_SinDatos_RetornaListaVacia()
    {
        _repository.GetTopUsersAsync(Arg.Any<DashboardTopUsersFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TopUserRow>());

        var result = await _sut.HandleAsync(new GetDashboardTopUsersQuery(TenantId, From, To, null));

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task AC2_TopUsers_FiltroUserIdsSeTransmiteAlRepositorio()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        _repository.GetTopUsersAsync(Arg.Any<DashboardTopUsersFilter>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TopUserRow>());

        await _sut.HandleAsync(new GetDashboardTopUsersQuery(TenantId, From, To, userIds));

        await _repository.Received(1).GetTopUsersAsync(
            Arg.Is<DashboardTopUsersFilter>(f =>
                f.TenantId == TenantId
                && f.UserIds != null
                && f.UserIds.Count == 2),
            5,
            Arg.Any<CancellationToken>());
    }
}
