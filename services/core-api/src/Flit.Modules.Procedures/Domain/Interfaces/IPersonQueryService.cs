namespace Flit.Modules.Procedures.Domain.Interfaces;

public sealed record PersonCaptureResult(
    bool Found,
    string QueryResultsJson,
    IReadOnlyList<string> Warnings,
    string? FullName);

public interface IPersonQueryService
{
    Task<PersonCaptureResult> QueryByDocumentAsync(
        Guid tenantId, string documentNumber, CancellationToken ct = default);
}
