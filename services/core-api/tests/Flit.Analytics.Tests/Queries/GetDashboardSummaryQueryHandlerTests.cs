using FluentAssertions;
using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Analytics.Tests.Queries;

/// <summary>HU-9794 — GET /api/v1/dashboard/summary</summary>
public class GetDashboardSummaryQueryHandlerTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();
    private readonly GetDashboardSummaryQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public GetDashboardSummaryQueryHandlerTests()
    {
        _sut = new GetDashboardSummaryQueryHandler(_repository);
    }

    [Fact]
    public async Task AC1_Summary_RetornaKpisPorFamiliaYEstados()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureSummaryRow("traspasos", "submitted", 30),
                new ProcedureSummaryRow("traspasos", "approved", 10),
                new ProcedureSummaryRow("matricula_inicial", "draft", 5),
                new ProcedureSummaryRow("otros", "rejected", 2)
            ]);

        var result = await _sut.HandleAsync(new GetDashboardSummaryQuery(TenantId, From, To));

        result.Period.From.Should().Be(From);
        result.Period.To.Should().Be(To);
        result.Summary.Total.Should().Be(47);
        result.Summary.ByFamily.Should().HaveCount(3);

        var traspasos = result.Summary.ByFamily.Single(f => f.Family == "traspasos");
        traspasos.Count.Should().Be(40);
        traspasos.Pct.Should().Be(85.11m);
        traspasos.ByStatus.Submitted.Should().Be(30);
        traspasos.ByStatus.Approved.Should().Be(10);

        result.Summary.ByStatus.Draft.Should().Be(5);
        result.Summary.ByStatus.Rejected.Should().Be(2);
    }

    [Fact]
    public async Task AC1_Summary_SinDatos_RetornaFamiliasEnCero()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProcedureSummaryRow>());

        var result = await _sut.HandleAsync(new GetDashboardSummaryQuery(TenantId, From, To));

        result.Summary.Total.Should().Be(0);
        result.Summary.ByFamily.Should().OnlyContain(f => f.Count == 0 && f.Pct == 0m);
        result.Summary.ByStatus.Should().BeEquivalentTo(new { Draft = 0, Submitted = 0, Approved = 0, Rejected = 0 });
    }

    [Fact]
    public async Task AC1_Summary_FiltroTenantSeTransmiteAlRepositorio()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProcedureSummaryRow>());

        await _sut.HandleAsync(new GetDashboardSummaryQuery(TenantId, From, To));

        await _repository.Received(1).GetSummaryByFamilyAndStatusAsync(
            Arg.Is<DashboardSummaryFilter>(f =>
                f.TenantId == TenantId && f.From == From && f.To == To),
            Arg.Any<CancellationToken>());
    }
}
