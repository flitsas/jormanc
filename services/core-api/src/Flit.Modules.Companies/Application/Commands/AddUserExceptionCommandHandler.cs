using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de AddUserExceptionCommand.
/// Verifica que la compañía exista, que el usuario no esté ya en la lista blanca
/// y persiste la excepción.
/// AC2 HU-9776.
/// </summary>
public sealed class AddUserExceptionCommandHandler(
    ICompanyRepository companyRepository,
    IClock clock)
{
    public async Task<Result<UserExceptionDto, CompanyError>> HandleAsync(
        AddUserExceptionCommand command, CancellationToken ct = default)
    {
        var company = await companyRepository.FindByIdAsync(command.CompanyId, ct);
        if (company is null)
            return Result<UserExceptionDto, CompanyError>.Failure(CompanyError.NotFound);

        var alreadyExists = await companyRepository.UserExceptionExistsAsync(
            command.CompanyId, command.UserId, ct);
        if (alreadyExists)
            return Result<UserExceptionDto, CompanyError>.Failure(CompanyError.UserExceptionAlreadyExists);

        var exception = new TenantUserException
        {
            Id = Guid.NewGuid(),
            CompanyId = command.CompanyId,
            UserId = command.UserId,
            AddedBy = command.AddedByUserId,
            AddedAt = clock.UtcNow
        };

        await companyRepository.AddUserExceptionAsync(exception, ct);

        return Result<UserExceptionDto, CompanyError>.Success(new UserExceptionDto(
            Id: exception.Id,
            CompanyId: exception.CompanyId,
            UserId: exception.UserId,
            AddedBy: exception.AddedBy,
            AddedAt: exception.AddedAt));
    }
}
