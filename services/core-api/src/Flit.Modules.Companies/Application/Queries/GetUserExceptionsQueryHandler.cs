using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Handler de GetUserExceptionsQuery.
/// AC2 HU-9776.
/// </summary>
public sealed class GetUserExceptionsQueryHandler(ICompanyRepository companyRepository)
{
    public async Task<Result<IReadOnlyList<UserExceptionDto>, CompanyError>> HandleAsync(
        GetUserExceptionsQuery query, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(query.CompanyId, ct);
        if (company is null)
            return Result<IReadOnlyList<UserExceptionDto>, CompanyError>.Failure(CompanyError.NotFound);

        var exceptions = await companyRepository.GetUserExceptionsAsync(query.CompanyId, ct);

        var dtos = exceptions.Select(e => new UserExceptionDto(
            Id: e.Id,
            CompanyId: e.CompanyId,
            UserId: e.UserId,
            AddedBy: e.AddedBy,
            AddedAt: e.AddedAt))
            .ToList();

        return Result<IReadOnlyList<UserExceptionDto>, CompanyError>.Success(dtos);
    }
}
