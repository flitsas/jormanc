using FluentAssertions;
using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Analytics.Tests.Queries;

/// <summary>HU-9794 — GET /api/v1/dashboard/procedures</summary>
public class GetDashboardProceduresQueryHandlerTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();
    private readonly GetDashboardProceduresQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public GetDashboardProceduresQueryHandlerTests()
    {
        _sut = new GetDashboardProceduresQueryHandler(_repository);
    }

    [Fact]
    public async Task AC2_Procedures_RetornaPaginaConDetalle()
    {
        var submittedAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero);

        _repository.GetProceduresDetailAsync(Arg.Any<DashboardProceduresFilter>(), Arg.Any<CancellationToken>())
            .Returns((
                new List<ProcedureDetailRow>
                {
                    new(
                        "TRASP-02_EVE-8841",
                        submittedAt,
                        "approved",
                        "ABC123",
                        "Juan Pérez",
                        submittedAt.AddDays(1),
                        updatedAt)
                },
                1));

        var result = await _sut.HandleAsync(new GetDashboardProceduresQuery(
            TenantId, From, To, "traspasos", null, null, 1, 20));

        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be("TRASP-02_EVE-8841");
        result.Data[0].Plate.Should().Be("ABC123");
        result.Data[0].OwnerName.Should().Be("Juan Pérez");
        result.Data[0].Status.Should().Be("approved");
    }

    [Fact]
    public async Task AC2_Procedures_FiltrosSeTransmitedAlRepositorio()
    {
        var userId = Guid.NewGuid();
        _repository.GetProceduresDetailAsync(Arg.Any<DashboardProceduresFilter>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<ProcedureDetailRow>(), 0));

        await _sut.HandleAsync(new GetDashboardProceduresQuery(
            TenantId,
            From,
            To,
            Family: "matricula_inicial",
            Status: "draft",
            UserIds: [userId],
            Page: 2,
            PageSize: 10));

        await _repository.Received(1).GetProceduresDetailAsync(
            Arg.Is<DashboardProceduresFilter>(f =>
                f.TenantId == TenantId
                && f.From == From
                && f.To == To
                && f.Family == "matricula_inicial"
                && f.Status == "draft"
                && f.UserIds!.Single() == userId
                && f.Page == 2
                && f.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_Procedures_PageSizeMaximo50()
    {
        _repository.GetProceduresDetailAsync(Arg.Any<DashboardProceduresFilter>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<ProcedureDetailRow>(), 0));

        await _sut.HandleAsync(new GetDashboardProceduresQuery(
            TenantId, From, To, null, null, null, 1, 100));

        await _repository.Received(1).GetProceduresDetailAsync(
            Arg.Is<DashboardProceduresFilter>(f => f.PageSize == 50),
            Arg.Any<CancellationToken>());
    }
}
