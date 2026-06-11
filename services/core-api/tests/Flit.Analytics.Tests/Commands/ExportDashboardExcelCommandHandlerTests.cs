using ClosedXML.Excel;
using FluentAssertions;
using Flit.Modules.Analytics.Application.Commands;
using Flit.Modules.Analytics.Domain.Interfaces;
using Flit.Modules.Analytics.Infrastructure.Excel;
using NSubstitute;
using Xunit;

namespace Flit.Analytics.Tests.Commands;

/// <summary>HU-9795 — GET /api/v1/dashboard/export/excel</summary>
public class ExportDashboardExcelCommandHandlerTests
{
    private readonly IAnalyticsRepository _repository = Substitute.For<IAnalyticsRepository>();
    private readonly ExportDashboardExcelCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

    public ExportDashboardExcelCommandHandlerTests()
    {
        _sut = new ExportDashboardExcelCommandHandler(_repository);
    }

    [Fact]
    public async Task AC1_Excel_UsaBatchesDe500YColumnasEsperadas()
    {
        var batch1 = Enumerable.Range(1, 500)
            .Select(i => CreateRow($"TRASP-{i:D4}"))
            .ToList();
        var batch2 = Enumerable.Range(501, 100)
            .Select(i => CreateRow($"TRASP-{i:D4}"))
            .ToList();

        _repository.StreamProceduresBatchesAsync(
                Arg.Any<DashboardExportFilter>(),
                ProceduresExcelExporter.DefaultBatchSize,
                Arg.Any<CancellationToken>())
            .Returns(_ => ToAsyncEnumerable(batch1, batch2));

        var result = await _sut.HandleAsync(new ExportDashboardExcelCommand(
            TenantId, From, To, null, null, null));

        result.ContentType.Should().Be(ExportDashboardExcelCommandHandler.XlsxContentType);
        result.Filename.Should().EndWith(".xlsx");

        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        sheet.Cell(1, 1).GetString().Should().Be("ID");
        sheet.Cell(1, 2).GetString().Should().Be("fecha_radicación");
        sheet.Cell(1, 3).GetString().Should().Be("estado");
        sheet.Cell(1, 4).GetString().Should().Be("placa");
        sheet.Cell(1, 5).GetString().Should().Be("propietario");
        sheet.Cell(1, 6).GetString().Should().Be("fecha_aprobación");
        sheet.Cell(1, 7).GetString().Should().Be("actualización");

        sheet.LastRowUsed()!.RowNumber().Should().Be(601);
        sheet.Cell(2, 1).GetString().Should().Be("TRASP-0001");
        sheet.Cell(601, 1).GetString().Should().Be("TRASP-0600");

        _repository.Received(1).StreamProceduresBatchesAsync(
            Arg.Is<DashboardExportFilter>(f => f.TenantId == TenantId),
            ProceduresExcelExporter.DefaultBatchSize,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_Excel_FiltroTenantSeTransmiteAlRepositorio()
    {
        _repository.StreamProceduresBatchesAsync(
                Arg.Any<DashboardExportFilter>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => ToAsyncEnumerable(Array.Empty<ProcedureDetailRow>()));

        await _sut.HandleAsync(new ExportDashboardExcelCommand(
            TenantId, From, To, "traspasos", "approved", [Guid.NewGuid()]));

        _repository.Received(1).StreamProceduresBatchesAsync(
            Arg.Is<DashboardExportFilter>(f =>
                f.TenantId == TenantId
                && f.From == From
                && f.To == To
                && f.Family == "traspasos"
                && f.Status == "approved"
                && f.UserIds!.Count == 1),
            ProceduresExcelExporter.DefaultBatchSize,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC1_Csv_RetornaTextCsvConEncabezados()
    {
        _repository.StreamProceduresBatchesAsync(
                Arg.Any<DashboardExportFilter>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => ToAsyncEnumerable(new List<ProcedureDetailRow> { CreateRow("TRASP-99") }));

        var result = await _sut.HandleAsync(new ExportDashboardExcelCommand(
            TenantId, From, To, null, null, null, Format: "csv"));

        result.ContentType.Should().Be("text/csv");
        result.Filename.Should().EndWith(".csv");

        var text = System.Text.Encoding.UTF8.GetString(result.Content);
        text.Should().StartWith("ID,fecha_radicación,estado,placa,propietario,fecha_aprobación,actualización");
        text.Should().Contain("TRASP-99");
    }

    private static ProcedureDetailRow CreateRow(string compositeId) =>
        new(
            compositeId,
            new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero),
            "approved",
            "ABC123",
            "Juan Pérez",
            new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 3, 10, 0, 0, TimeSpan.Zero));

    private static async IAsyncEnumerable<IReadOnlyList<ProcedureDetailRow>> ToAsyncEnumerable(
        params IReadOnlyList<ProcedureDetailRow>[] batches)
    {
        foreach (var batch in batches)
            yield return batch;
        await Task.CompletedTask;
    }
}
