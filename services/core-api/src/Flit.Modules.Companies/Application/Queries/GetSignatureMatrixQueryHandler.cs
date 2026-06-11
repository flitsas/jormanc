using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Handler de GetSignatureMatrixQuery.
/// AC1 HU-9776.
/// </summary>
public sealed class GetSignatureMatrixQueryHandler(ICompanyRepository companyRepository)
{
    public async Task<Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>> HandleAsync(
        GetSignatureMatrixQuery query, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(query.CompanyId, ct);
        if (company is null)
            return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Failure(CompanyError.NotFound);

        var entries = await companyRepository.GetSignatureMatrixAsync(query.CompanyId, ct);

        var dtos = entries.Select(e => new SignatureMatrixEntryDto(
            Id: e.Id,
            CompanyId: e.CompanyId,
            ActorRole: e.ActorRole,
            SignatureType: e.SignatureType,
            IsActive: e.IsActive))
            .ToList();

        return Result<IReadOnlyList<SignatureMatrixEntryDto>, CompanyError>.Success(dtos);
    }
}
