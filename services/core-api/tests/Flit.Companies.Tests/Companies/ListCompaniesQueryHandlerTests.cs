using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Companies.Application.Queries;
using Flit.Modules.Companies.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Companies.Tests.Companies;

/// <summary>
/// Tests unitarios para ListCompaniesQueryHandler.
/// AC2 HU-9774: GET /api/v1/admin/companies/index con paginación server-side y filtros.
/// </summary>
public class ListCompaniesQueryHandlerTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly ListCompaniesQueryHandler _sut;

    public ListCompaniesQueryHandlerTests()
    {
        _sut = new ListCompaniesQueryHandler(_companyRepo);
    }

    // ─── AC2: paginación y respuesta ─────────────────────────────────────────

    [Fact]
    public async Task AC2_ListarCompanias_RetornaPageDto()
    {
        // Arrange
        var companies = BuildCompanies(3, "acme", "beta", "gamma");
        _companyRepo.ListAsync(Arg.Any<CompanyListFilter>(), Arg.Any<CancellationToken>())
            .Returns((companies, 3));

        var query = new ListCompaniesQuery(Page: 1, PageSize: 20,
            Nit: null, Name: null, Status: null, CreatedFrom: null, CreatedTo: null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Total.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task AC2_ListarCompanias_DtoContieneSlugDeTenant()
    {
        // Arrange
        var companies = BuildCompanies(1, "test-slug");
        _companyRepo.ListAsync(Arg.Any<CompanyListFilter>(), Arg.Any<CancellationToken>())
            .Returns((companies, 1));

        var query = new ListCompaniesQuery(1, 20, null, null, null, null, null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Data[0].TenantSlug.Should().Be("test-slug");
    }

    [Fact]
    public async Task AC2_ListarCompanias_FiltrosSeTransmitedAlRepositorio()
    {
        // Arrange
        _companyRepo.ListAsync(Arg.Any<CompanyListFilter>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var createdFrom = DateTimeOffset.UtcNow.AddDays(-7);
        var query = new ListCompaniesQuery(2, 10, "900", "Acme", "active", createdFrom, null);

        // Act
        await _sut.HandleAsync(query);

        // Assert: el repositorio recibe exactamente los filtros del query
        await _companyRepo.Received(1).ListAsync(
            Arg.Is<CompanyListFilter>(f =>
                f.Page == 2 &&
                f.PageSize == 10 &&
                f.Nit == "900" &&
                f.Name == "Acme" &&
                f.Status == "active" &&
                f.CreatedFrom == createdFrom),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_ListarCompanias_SinResultados_RetornaPaginaVacia()
    {
        // Arrange
        _companyRepo.ListAsync(Arg.Any<CompanyListFilter>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Company>(), 0));

        var query = new ListCompaniesQuery(1, 20, null, null, null, null, null);

        // Act
        var result = await _sut.HandleAsync(query);

        // Assert
        result.Total.Should().Be(0);
        result.Data.Should().BeEmpty();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<Company> BuildCompanies(int count, params string[] slugs)
    {
        return Enumerable.Range(0, count).Select(i => new Company
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Nit = $"90000000{i}-{i}",
            Name = $"Company {i}",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.NewGuid(),
            Tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Slug = i < slugs.Length ? slugs[i] : $"slug-{i}",
                Name = $"Tenant {i}",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        }).ToList();
    }
}
