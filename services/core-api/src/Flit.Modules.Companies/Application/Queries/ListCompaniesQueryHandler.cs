using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Interfaces;

namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Handler de ListCompaniesQuery.
/// Devuelve una página de compañías según los filtros recibidos.
/// AC2 HU-9774.
/// </summary>
public sealed class ListCompaniesQueryHandler(ICompanyRepository companyRepository)
{
    public async Task<CompanyPageDto> HandleAsync(
        ListCompaniesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await companyRepository.ListAsync(query.ToFilter(), ct);

        var dtos = items.Select(c => new CompanyListItemDto(
            Id: c.Id,
            TenantId: c.TenantId,
            Nit: c.Nit,
            Name: c.Name,
            Status: c.Status,
            TenantSlug: c.Tenant?.Slug ?? string.Empty,
            CreatedAt: c.CreatedAt)).ToList();

        return new CompanyPageDto(
            Data: dtos,
            Total: total,
            Page: query.Page,
            PageSize: query.PageSize);
    }
}
