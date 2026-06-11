using System.Text.RegularExpressions;

namespace Flit.Modules.Documents.Domain.Services;

public static partial class HtmlSanitizer
{
    [GeneratedRegex("<script\\b[^<]*(?:(?!<\\/script>)<[^<]*)*<\\/script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    public static string Sanitize(string html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : ScriptTagRegex().Replace(html, string.Empty);
}
