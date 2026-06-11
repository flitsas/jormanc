using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flit.Modules.Companies.Infrastructure.Persistence;

/// <summary>
/// Implementación del repositorio de compañías usando FlitDbContext (EF Core).
/// </summary>
public sealed class CompanyRepository(FlitDbContext db) : ICompanyRepository
{
    public Task<bool> NitExistsAsync(string nit, CancellationToken ct = default) =>
        db.Companies.AnyAsync(c => c.Nit == nit, ct);

    public async Task<(IReadOnlyList<Company> Items, int Total)> ListAsync(
        CompanyListFilter filter, CancellationToken ct = default)
    {
        var query = db.Companies
            .Include(c => c.Tenant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nit))
            query = query.Where(c => c.Nit.Contains(filter.Nit));

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(c => c.Name.ToLower().Contains(filter.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(c => c.Status == filter.Status);

        if (filter.CreatedFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= filter.CreatedFrom.Value);

        if (filter.CreatedTo.HasValue)
            query = query.Where(c => c.CreatedAt <= filter.CreatedTo.Value);

        var total = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Company?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Companies
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Company?> FindByIdWithConfigAsync(Guid id, CancellationToken ct = default) =>
        db.Companies
            .Include(c => c.Config)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task CreateAsync(Company company, CompanyConfig config, CancellationToken ct = default)
    {
        db.Companies.Add(company);
        db.CompanyConfigs.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateConfigAsync(CompanyConfig config, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    // ── Signature Matrix (AC1 HU-9776) ───────────────────────────────────────

    public async Task<IReadOnlyList<CompanySignatureMatrix>> GetSignatureMatrixAsync(
        Guid companyId, CancellationToken ct = default) =>
        await db.CompanySignatureMatrices
            .Where(m => m.CompanyId == companyId)
            .ToListAsync(ct);

    public async Task ReplaceSignatureMatrixAsync(
        Guid companyId, IEnumerable<CompanySignatureMatrix> entries, CancellationToken ct = default)
    {
        var existing = await db.CompanySignatureMatrices
            .Where(m => m.CompanyId == companyId)
            .ToListAsync(ct);

        db.CompanySignatureMatrices.RemoveRange(existing);
        db.CompanySignatureMatrices.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }

    // ── User Exceptions (AC2 HU-9776) ────────────────────────────────────────

    public async Task<IReadOnlyList<TenantUserException>> GetUserExceptionsAsync(
        Guid companyId, CancellationToken ct = default) =>
        await db.TenantUserExceptions
            .Where(e => e.CompanyId == companyId)
            .OrderBy(e => e.AddedAt)
            .ToListAsync(ct);

    public Task<bool> UserExceptionExistsAsync(
        Guid companyId, Guid userId, CancellationToken ct = default) =>
        db.TenantUserExceptions
            .AnyAsync(e => e.CompanyId == companyId && e.UserId == userId, ct);

    public async Task AddUserExceptionAsync(TenantUserException exception, CancellationToken ct = default)
    {
        db.TenantUserExceptions.Add(exception);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveUserExceptionAsync(
        Guid companyId, Guid userId, CancellationToken ct = default)
    {
        var exception = await db.TenantUserExceptions
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.UserId == userId, ct);

        if (exception is null)
            return false;

        db.TenantUserExceptions.Remove(exception);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── OT Enabled (AC3 HU-9776) ─────────────────────────────────────────────

    public async Task<IReadOnlyList<CompanyOtEnabled>> GetOtEnabledAsync(
        Guid companyId, CancellationToken ct = default) =>
        await db.CompanyOtEnabled
            .Where(e => e.CompanyId == companyId)
            .ToListAsync(ct);

    public async Task ReplaceOtEnabledAsync(
        Guid companyId, IEnumerable<CompanyOtEnabled> entries, CancellationToken ct = default)
    {
        var existing = await db.CompanyOtEnabled
            .Where(e => e.CompanyId == companyId)
            .ToListAsync(ct);

        db.CompanyOtEnabled.RemoveRange(existing);
        db.CompanyOtEnabled.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }
}
