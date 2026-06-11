namespace Flit.Modules.Procedures.Domain.Interfaces;

public sealed record LegalEntityCaptureResult(
    bool Found,
    string CompanyName,
    string Nit,
    string RepresentativeName,
    string RepresentativeDocument,
    string RuesPayloadJson,
    IReadOnlyList<string> Warnings);

public interface ILegalEntityQueryService
{
    Task<LegalEntityCaptureResult> QueryByNitAsync(
        Guid tenantId, string nit, CancellationToken ct = default);
}
