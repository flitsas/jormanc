namespace Flit.Modules.Documents.Domain.Services;

public static class MarkerMetadataParser
{
    public static (string DataSource, string DataPath) Parse(string marker)
    {
        if (marker.StartsWith("actor[", StringComparison.Ordinal))
        {
            var closeBracket = marker.IndexOf(']');
            if (closeBracket > 6 && marker.Length > closeBracket + 1 && marker[closeBracket + 1] == '.')
            {
                var role = marker[6..closeBracket];
                var path = marker[(closeBracket + 2)..];
                return ("actor", $"{role}.{path}");
            }
        }

        var dotIndex = marker.IndexOf('.');
        if (dotIndex <= 0) return ("unknown", marker);

        return (marker[..dotIndex], marker[(dotIndex + 1)..]);
    }
}
