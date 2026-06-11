using System.Globalization;
using System.Text;
using Flit.Modules.Analytics.Domain.Interfaces;

namespace Flit.Modules.Analytics.Infrastructure.Excel;

/// <summary>Export CSV streaming para datasets grandes (alternativa a xlsx).</summary>
public static class ProceduresCsvExporter
{
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

    public static string HeaderLine => string.Join(',', HeaderColumns.Select(Escape));

    public static string FormatRow(ProcedureDetailRow row)
    {
        var values = new[]
        {
            row.CompositeId,
            row.SubmittedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            row.Status,
            row.Plate ?? string.Empty,
            row.OwnerName ?? string.Empty,
            row.ApprovedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            row.UpdatedAt.ToString("o", CultureInfo.InvariantCulture)
        };

        return string.Join(',', values.Select(Escape));
    }

    public static string BuildFilename(DateTimeOffset from, DateTimeOffset to) =>
        $"tramites-{from:yyyyMMdd}-{to:yyyyMMdd}.csv";

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }
}
