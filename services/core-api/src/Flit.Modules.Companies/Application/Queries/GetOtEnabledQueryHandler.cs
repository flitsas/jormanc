using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Handler de GetOtEnabledQuery.
/// AC3 HU-9776.
/// </summary>
public sealed class GetOtEnabledQueryHandler(ICompanyRepository companyRepository)
{
    public async Task<Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>> HandleAsync(
        GetOtEnabledQuery query, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(query.CompanyId, ct);
        if (company is null)
            return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Failure(CompanyError.NotFound);

        var entries = await companyRepository.GetOtEnabledAsync(query.CompanyId, ct);

        var dtos = entries.Select(e => new OtEnabledEntryDto(
            Id: e.Id,
            CompanyId: e.CompanyId,
            OtSlug: e.OtSlug,
            ProcedureFamily: e.ProcedureFamily,
            IsEnabled: e.IsEnabled))
            .ToList();

        return Result<IReadOnlyList<OtEnabledEntryDto>, CompanyError>.Success(dtos);
    }
}
