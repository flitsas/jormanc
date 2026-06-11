namespace Flit.Modules.Integrations.Domain.Models;

/// <summary>
/// Resultado de consulta de restricciones activas sobre placa o documento.
/// </summary>
public sealed record RestrictionQueryResult(
    bool Found,
    IReadOnlyList<RestrictionItem> Restrictions,
    string? RawJson);

public sealed record RestrictionItem(
    string Type,
    string Description,
    DateOnly? ExpiresAt);
