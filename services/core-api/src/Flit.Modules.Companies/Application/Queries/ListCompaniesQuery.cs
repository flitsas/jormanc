using Flit.Modules.Companies.Domain.Interfaces;

namespace Flit.Modules.Companies.Application.Queries;

/// <summary>
/// Consulta de lista de compañías con filtros y paginación server-side.
/// AC2 HU-9774.
/// </summary>
public sealed record ListCompaniesQuery(
    int Page,
    int PageSize,
    string? Nit,
    string? Name,
    string? Status,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo)
{
    public CompanyListFilter ToFilter() =>
        new(Page, PageSize, Nit, Name, Status, CreatedFrom, CreatedTo);
}
