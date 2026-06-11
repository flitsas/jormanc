using System.Text.Json;
using Flit.Modules.Procedures.Domain.Interfaces;

namespace Flit.Modules.Procedures.Infrastructure.Services;

/// <summary>
/// Consulta RUES mock para DEV — sustituible por IRuesConnector cuando esté disponible.
/// </summary>
public sealed class LegalEntityQueryService : ILegalEntityQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Task<LegalEntityCaptureResult> QueryByNitAsync(
        Guid tenantId, string nit, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            source = "rues",
            nit,
            companyName = "MOCK EMPRESA SA",
            representative = new { name = "Juan Pérez", document = "11111111" }
        }, JsonOptions);

        return Task.FromResult(new LegalEntityCaptureResult(
            Found: true,
            CompanyName: "MOCK EMPRESA SA",
            Nit: nit,
            RepresentativeName: "Juan Pérez",
            RepresentativeDocument: "11111111",
            RuesPayloadJson: payload,
            Warnings: []));
    }
}
