namespace Flit.Modules.Integrations.Domain.Models;

/// <summary>
/// Resultado de una consulta de persona (conductor/propietario) al RUNT.
/// @pii:high — contiene datos personales.
/// </summary>
public sealed record PersonQueryResult(
    bool Found,
    string? DocumentNumber,
    string? FullName,
    string? LicenseCategory,
    string? LicenseStatus,
    DateOnly? LicenseExpiry,
    IReadOnlyList<string> Restrictions,
    string? RawJson);
