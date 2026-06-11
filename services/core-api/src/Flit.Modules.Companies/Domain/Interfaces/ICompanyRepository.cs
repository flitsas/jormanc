using Flit.Infrastructure.Persistence.Entities.Companies;

namespace Flit.Modules.Companies.Domain.Interfaces;

/// <summary>
/// Contrato del repositorio de compañías.
/// Implementación en Infrastructure/Persistence/CompanyRepository.cs.
/// </summary>
public interface ICompanyRepository
{
    Task<bool> NitExistsAsync(string nit, CancellationToken ct = default);

    Task<(IReadOnlyList<Company> Items, int Total)> ListAsync(
        CompanyListFilter filter, CancellationToken ct = default);

    Task<Company?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<Company?> FindByIdWithConfigAsync(Guid id, CancellationToken ct = default);

    Task CreateAsync(Company company, CompanyConfig config, CancellationToken ct = default);

    Task UpdateConfigAsync(CompanyConfig config, CancellationToken ct = default);

    // ── Signature Matrix (AC1 HU-9776) ───────────────────────────────────────
    Task<IReadOnlyList<CompanySignatureMatrix>> GetSignatureMatrixAsync(
        Guid companyId, CancellationToken ct = default);

    Task ReplaceSignatureMatrixAsync(
        Guid companyId, IEnumerable<CompanySignatureMatrix> entries, CancellationToken ct = default);

    // ── User Exceptions (AC2 HU-9776) ────────────────────────────────────────
    Task<IReadOnlyList<TenantUserException>> GetUserExceptionsAsync(
        Guid companyId, CancellationToken ct = default);

    Task<bool> UserExceptionExistsAsync(
        Guid companyId, Guid userId, CancellationToken ct = default);

    Task AddUserExceptionAsync(TenantUserException exception, CancellationToken ct = default);

    Task<bool> RemoveUserExceptionAsync(
        Guid companyId, Guid userId, CancellationToken ct = default);

    // ── OT Enabled (AC3 HU-9776) ─────────────────────────────────────────────
    Task<IReadOnlyList<CompanyOtEnabled>> GetOtEnabledAsync(
        Guid companyId, CancellationToken ct = default);

    Task ReplaceOtEnabledAsync(
        Guid companyId, IEnumerable<CompanyOtEnabled> entries, CancellationToken ct = default);
}

/// <summary>
/// Filtros para la lista de compañías (AC2).
/// </summary>
public sealed record CompanyListFilter(
    int Page,
    int PageSize,
    string? Nit,
    string? Name,
    string? Status,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);
