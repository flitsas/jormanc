using System.Text;
using Flit.Modules.Analytics.Domain.Interfaces;
using Flit.Modules.Analytics.Infrastructure.Excel;

namespace Flit.Modules.Analytics.Application.Commands;

/// <summary>HU-9795 AC1 — export Excel/CSV por chunks de 500 filas.</summary>
public sealed class ExportDashboardExcelCommandHandler(IAnalyticsRepository analyticsRepository)
{
    public const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<ExportFileResult> HandleAsync(
        ExportDashboardExcelCommand command,
        CancellationToken ct = default)
    {
        var filter = new DashboardExportFilter(
            command.TenantId,
            command.From,
            command.To,
            command.Family,
            command.Status,
            command.UserIds);

        if (string.Equals(command.Format, "csv", StringComparison.OrdinalIgnoreCase))
            return await ExportCsvAsync(filter, ct);

        return await ExportXlsxAsync(filter, ct);
    }

    private async Task<ExportFileResult> ExportXlsxAsync(
        DashboardExportFilter filter,
        CancellationToken ct)
    {
        using var exporter = new ProceduresExcelExporter();

        await foreach (var batch in analyticsRepository.StreamProceduresBatchesAsync(
                           filter,
                           ProceduresExcelExporter.DefaultBatchSize,
                           ct))
        {
            exporter.AppendRows(batch);
        }

        return new ExportFileResult(
            exporter.ToByteArray(),
            XlsxContentType,
            ProceduresExcelExporter.BuildFilename(filter.From, filter.To));
    }

    private async Task<ExportFileResult> ExportCsvAsync(
        DashboardExportFilter filter,
        CancellationToken ct)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ProceduresCsvExporter.HeaderLine);

        await foreach (var batch in analyticsRepository.StreamProceduresBatchesAsync(
                           filter,
                           ProceduresExcelExporter.DefaultBatchSize,
                           ct))
        {
            foreach (var row in batch)
                builder.AppendLine(ProceduresCsvExporter.FormatRow(row));
        }

        return new ExportFileResult(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/csv",
            ProceduresCsvExporter.BuildFilename(filter.From, filter.To));
    }
}
