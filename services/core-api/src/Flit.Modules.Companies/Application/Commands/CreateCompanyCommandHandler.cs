using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de CreateCompanyCommand.
/// Flujo: valida NIT único → crea tenant → persiste company + config por defecto.
/// AC1 HU-9774.
/// </summary>
public sealed class CreateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ITenantService tenantService,
    IClock clock)
{
    public async Task<Result<CompanyDto, CompanyError>> HandleAsync(
        CreateCompanyCommand command, CancellationToken ct = default)
    {
        if (await companyRepository.NitExistsAsync(command.Nit, ct))
            return Result<CompanyDto, CompanyError>.Failure(CompanyError.NitAlreadyExists);

        if (await tenantService.SlugExistsAsync(command.TenantSlug, ct))
            return Result<CompanyDto, CompanyError>.Failure(CompanyError.TenantSlugAlreadyExists);

        var tenantId = await tenantService.CreateTenantAsync(command.TenantSlug, command.Name, ct);

        var now = clock.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Nit = command.Nit,
            Name = command.Name,
            Status = "active",
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        var config = new CompanyConfig
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            OnlyOwnVehicles = false,
            BaulFirmasEnabled = false,
            NotificationTarget = "radicador",
            SmtpMode = "native",
            MatriculaConfig = "{}",
            TraspasosConfig = "{}",
            ContingencyConfig = "{}",
            RecaudoMethods = "[]",
            UpdatedAt = now
        };

        await companyRepository.CreateAsync(company, config, ct);

        return Result<CompanyDto, CompanyError>.Success(new CompanyDto(
            Id: company.Id,
            TenantId: tenantId,
            TenantSlug: command.TenantSlug,
            Nit: company.Nit,
            Name: company.Name,
            Status: company.Status,
            CreatedAt: company.CreatedAt));
    }
}
