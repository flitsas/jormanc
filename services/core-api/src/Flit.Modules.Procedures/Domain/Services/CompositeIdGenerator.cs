namespace Flit.Modules.Procedures.Domain.Services;

/// <summary>Genera composite_id único: TRASP-02_EVE-8841</summary>
public static class CompositeIdGenerator
{
    public static string Generate(string family, string companySlug, int sequence)
    {
        var prefix = family switch
        {
            "traspasos" => "TRASP",
            "matricula_inicial" => "MATR",
            _ => "OTROS"
        };

        var slugPart = string.IsNullOrWhiteSpace(companySlug)
            ? "GEN"
            : companySlug.Length >= 3
                ? companySlug[..3].ToUpperInvariant()
                : companySlug.ToUpperInvariant();

        var ts = DateTimeOffset.UtcNow.ToString("HHmm");
        return $"{prefix}-{sequence:D2}_{slugPart}-{ts}";
    }
}
