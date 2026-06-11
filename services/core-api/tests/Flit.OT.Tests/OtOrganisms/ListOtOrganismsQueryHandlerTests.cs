using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Queries;
using Flit.Modules.OT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.OtOrganisms;

/// <summary>
/// AC3 HU-9798 — GET /api/v1/ot-organisms solo retorna OTs del tenant del JWT.
/// </summary>
public class ListOtOrganismsQueryHandlerTests
{
    private readonly IOtOrganismRepository _repository = Substitute.For<IOtOrganismRepository>();
    private readonly ListOtOrganismsQueryHandler _sut;

    public ListOtOrganismsQueryHandlerTests()
    {
        _sut = new ListOtOrganismsQueryHandler(_repository);
    }

    [Fact]
    public async Task AC3_ListarOTs_FiltraPorTenantId()
    {
        var tenantA = Guid.NewGuid();
        var organisms = new List<OtOrganism>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                Slug = "ot-a",
                Name = "OT Tenant A",
                Mode = "dashboard",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.NewGuid(),
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = Guid.NewGuid()
            }
        };

        _repository.ListByTenantAsync(tenantA, Arg.Any<CancellationToken>()).Returns(organisms);

        var result = await _sut.HandleAsync(new ListOtOrganismsQuery(tenantA));

        result.Should().HaveCount(1);
        result[0].TenantId.Should().Be(tenantA);
        await _repository.Received(1).ListByTenantAsync(tenantA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC3_ListarOTs_TenantSinRegistros_RetornaVacio()
    {
        var tenantB = Guid.NewGuid();
        _repository.ListByTenantAsync(tenantB, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OtOrganism>());

        var result = await _sut.HandleAsync(new ListOtOrganismsQuery(tenantB));

        result.Should().BeEmpty();
    }
}
