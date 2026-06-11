using System.Text.Json;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Infrastructure;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

public sealed class PersonQueryService(ConnectorRouter connectorRouter) : IPersonQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PersonCaptureResult> QueryByDocumentAsync(
        Guid tenantId, string documentNumber, CancellationToken ct = default)
    {
        try
        {
            var (result, _, _) = await connectorRouter.ExecuteRuntAsync(
                tenantId,
                (connector, innerCt) => connector.QueryPersonAsync(documentNumber, innerCt),
                "runt.person",
                ct);

            var warnings = result.Restrictions
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => $"Restricción: {r.Trim().ToUpperInvariant()}")
                .ToList();

            var payload = JsonSerializer.Serialize(new
            {
                source = "runt",
                result.Found,
                result.DocumentNumber,
                result.FullName,
                result.LicenseCategory,
                result.LicenseStatus,
                result.LicenseExpiry,
                result.Restrictions,
                result.RawJson
            }, JsonOptions);

            return new PersonCaptureResult(
                Found: result.Found,
                QueryResultsJson: payload,
                Warnings: warnings,
                FullName: result.FullName);
        }
        catch (AllConnectorsFailedException)
        {
            return new PersonCaptureResult(false, "{}", [], null);
        }
    }
}
