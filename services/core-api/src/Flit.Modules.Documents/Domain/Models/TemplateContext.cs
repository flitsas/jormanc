using System.Collections.Frozen;
using System.Reflection;

namespace Flit.Modules.Documents.Domain.Models;

public sealed class ProcedureContextData
{
    public string? CompositeId { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}

public sealed class ActorContextData
{
    public string? FullName { get; init; }
    public string? DocumentNumber { get; init; }
    public string? Nit { get; init; }
    public decimal? CuotaPct { get; init; }
}

public sealed class VehicleContextData
{
    public string? Plate { get; init; }
    public IReadOnlyDictionary<string, string?> Runt { get; init; } =
        FrozenDictionary<string, string?>.Empty;

    public IReadOnlyDictionary<string, string?> Simit { get; init; } =
        FrozenDictionary<string, string?>.Empty;
}

public sealed class IdentityContextData
{
    public string? Verdict { get; init; }
    public DateTimeOffset? ValidatedAt { get; init; }
}

public sealed class OtContextData
{
    public string? Name { get; init; }
    public string? Code { get; init; }
}

/// <summary>Contexto de resolución de marcadores {{procedure.*}}, {{actor[*].*}}, {{vehicle.*}}.</summary>
public sealed class TemplateContext
{
    public ProcedureContextData Procedure { get; init; } = new();
    public IReadOnlyDictionary<string, ActorContextData> Actors { get; init; } =
        FrozenDictionary<string, ActorContextData>.Empty;
    public VehicleContextData? Vehicle { get; init; }
    public IdentityContextData? Identity { get; init; }
    public OtContextData? Ot { get; init; }

    public string? Resolve(string path)
    {
        path = path.Trim();
        if (path.Length == 0) return null;

        if (path.StartsWith("actor[", StringComparison.Ordinal))
        {
            var closeBracket = path.IndexOf(']');
            if (closeBracket <= 6) return null;

            var role = path[6..closeBracket];
            if (path.Length <= closeBracket + 1 || path[closeBracket + 1] != '.') return null;

            if (!Actors.TryGetValue(role, out var actor)) return null;
            return ResolveObjectPath(actor, path[(closeBracket + 2)..]);
        }

        if (path.StartsWith("vehicle.", StringComparison.Ordinal))
            return Vehicle is null ? null : ResolveObjectPath(Vehicle, path["vehicle.".Length..]);

        if (path.StartsWith("procedure.", StringComparison.Ordinal))
            return ResolveObjectPath(Procedure, path["procedure.".Length..]);

        if (path.StartsWith("identity.", StringComparison.Ordinal))
            return Identity is null ? null : ResolveObjectPath(Identity, path["identity.".Length..]);

        if (path.StartsWith("ot.", StringComparison.Ordinal))
            return Ot is null ? null : ResolveObjectPath(Ot, path["ot.".Length..]);

        return null;
    }

    private static string? ResolveObjectPath(object root, string remainder)
    {
        object? current = root;
        foreach (var segment in remainder.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is null) return null;

            if (current is IReadOnlyDictionary<string, string?> dict)
            {
                if (!dict.TryGetValue(segment, out var dictValue)) return null;
                current = dictValue;
                continue;
            }

            var prop = current.GetType().GetProperty(
                ToPascalCase(segment),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null) return null;
            current = prop.GetValue(current);
        }

        return current switch
        {
            null => null,
            DateTimeOffset dto => dto.ToString("O"),
            decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => current.ToString()
        };
    }

    private static string ToPascalCase(string segment) =>
        segment.Length switch
        {
            0 => segment,
            1 => segment.ToUpperInvariant(),
            _ => char.ToUpperInvariant(segment[0]) + segment[1..]
        };
}
