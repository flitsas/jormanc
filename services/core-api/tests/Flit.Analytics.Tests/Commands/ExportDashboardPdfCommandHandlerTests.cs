using FluentAssertions;
using Flit.Modules.Analytics.Application.Commands;
using Flit.Modules.Analytics.Application.DTOs;
using Flit.Modules.Analytics.Application.Queries;
using Flit.Modules.Analytics.Domain.Interfaces;
using Flit.Modules.Analytics.Infrastructure.Pdf;
using NSubstitute;
using Xunit;

namespace Flit.Analytics.Tests.Commands;

/// <summary>HU-9795 — GET /api/v1/dashboard/export/pdf</summary>
public class ExportDashboardPdfCommandHandlerTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();
    private readonly ExportDashboardPdfCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public ExportDashboardPdfCommandHandlerTests()
    {
        var summaryHandler = new GetDashboardSummaryQueryHandler(_repository);
        _sut = new ExportDashboardPdfCommandHandler(summaryHandler);
    }

    [Fact]
    public async Task AC2_Pdf_GeneraResumenEjecutivoConKpis()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureSummaryRow("traspasos", "approved", 40),
                new ProcedureSummaryRow("matricula_inicial", "draft", 10)
            ]);

        var result = await _sut.HandleAsync(new ExportDashboardPdfCommand(
            TenantId, From, To, null, IncludeCharts: true));

        result.ContentType.Should().Be(ExportDashboardPdfCommandHandler.PdfContentType);
        result.Filename.Should().StartWith("resumen-ejecutivo-").And.EndWith(".pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal([0x25, 0x50, 0x44, 0x46]); // %PDF
    }

    [Fact]
    public async Task AC2_Pdf_FiltraFamiliasSolicitadas()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns([
                new ProcedureSummaryRow("traspasos", "approved", 30),
                new ProcedureSummaryRow("matricula_inicial", "draft", 20),
                new ProcedureSummaryRow("otros", "submitted", 5)
            ]);

        var result = await _sut.HandleAsync(new ExportDashboardPdfCommand(
            TenantId, From, To, ["traspasos"], IncludeCharts: false));

        result.Content.Should().NotBeEmpty();

        await _repository.Received(1).GetSummaryByFamilyAndStatusAsync(
            Arg.Is<DashboardSummaryFilter>(f => f.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Pdf_TenantIdSeTransmiteAlRepositorio()
    {
        _repository.GetSummaryByFamilyAndStatusAsync(Arg.Any<DashboardSummaryFilter>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProcedureSummaryRow>());

        await _sut.HandleAsync(new ExportDashboardPdfCommand(TenantId, From, To, null));

        await _repository.Received(1).GetSummaryByFamilyAndStatusAsync(
            Arg.Is<DashboardSummaryFilter>(f =>
                f.TenantId == TenantId && f.From == From && f.To == To),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AC2_PdfFilename_SigueConvencionResumenEjecutivo()
    {
        ExecutiveSummaryTemplate.BuildFilename()
            .Should().MatchRegex(@"^resumen-ejecutivo-\d{8}\.pdf$");
    }
}
