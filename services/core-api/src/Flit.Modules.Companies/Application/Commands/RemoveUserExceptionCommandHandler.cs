using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de RemoveUserExceptionCommand.
/// Verifica que la compañía exista y que el usuario esté en la lista blanca antes de eliminarlo.
/// AC2 HU-9776.
/// </summary>
public sealed class RemoveUserExceptionCommandHandler(ICompanyRepository companyRepository)
{
    public async Task<Result<bool, CompanyError>> HandleAsync(
        RemoveUserExceptionCommand command, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(command.CompanyId, ct);
        if (company is null)
            return Result<bool, CompanyError>.Failure(CompanyError.NotFound);

        var removed = await companyRepository.RemoveUserExceptionAsync(
            command.CompanyId, command.UserId, ct);

        if (!removed)
            return Result<bool, CompanyError>.Failure(CompanyError.UserExceptionNotFound);

        return Result<bool, CompanyError>.Success(true);
    }
}
