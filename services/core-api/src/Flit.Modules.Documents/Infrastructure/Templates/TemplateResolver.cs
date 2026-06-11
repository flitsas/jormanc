using System.Text.RegularExpressions;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.Modules.Documents.Domain.Models;

namespace Flit.Modules.Documents.Infrastructure.Templates;

public sealed partial class TemplateResolver : ITemplateResolver
{
    [GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex MarkerRegex();

    public IReadOnlyList<string> DetectMarkers(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return [];

        return MarkerRegex().Matches(html)
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToArray();
    }

    public string Resolve(string html, TemplateContext context) =>
        MarkerRegex().Replace(html, match =>
        {
            var marker = match.Groups[1].Value.Trim();
            var value = context.Resolve(marker);
            return value ?? $"[{marker}:NO_DATA]";
        });
}
