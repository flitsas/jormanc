using ClosedXML.Excel;
using Flit.Modules.Analytics.Domain.Interfaces;

namespace Flit.Modules.Analytics.Infrastructure.Excel;

/// <summary>
/// Genera .xlsx por chunks sin cargar todos los registros en una sola query.
/// HU-9795 AC1 — batch size 500.
/// </summary>
public sealed class ProceduresExcelExporter : IDisposable
{
    public const int DefaultBatchSize = 500;

    private static readonly string[] HeaderColumns =
    [
        "ID",
        "fecha_radicación",
        "estado",
        "placa",
        "propietario",
        "fecha_aprobación",
        "actualización"
    ];

    private readonly XLWorkbook _workbook = new();
    private readonly IXLWorksheet _sheet;
    private int _nextRow = 2;

    public ProceduresExcelExporter()
    {
        _sheet = _workbook.AddWorksheet("Trámites");
        for (var col = 0; col < HeaderColumns.Length; col++)
            _sheet.Cell(1, col + 1).Value = HeaderColumns[col];

        var headerRange = _sheet.Range(1, 1, 1, HeaderColumns.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    public void AppendRows(IReadOnlyList<ProcedureDetailRow> rows)
    {
        foreach (var row in rows)
        {
            _sheet.Cell(_nextRow, 1).Value = row.CompositeId;
            _sheet.Cell(_nextRow, 2).Value = row.SubmittedAt?.ToString("o") ?? string.Empty;
            _sheet.Cell(_nextRow, 3).Value = row.Status;
            _sheet.Cell(_nextRow, 4).Value = row.Plate ?? string.Empty;
            _sheet.Cell(_nextRow, 5).Value = row.OwnerName ?? string.Empty;
            _sheet.Cell(_nextRow, 6).Value = row.ApprovedAt?.ToString("o") ?? string.Empty;
            _sheet.Cell(_nextRow, 7).Value = row.UpdatedAt.ToString("o");
            _nextRow++;
        }
    }

    public byte[] ToByteArray()
    {
        _sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        _workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public void Dispose() => _workbook.Dispose();

    public static string BuildFilename(DateTimeOffset from, DateTimeOffset to) =>
        $"tramites-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx";
}
