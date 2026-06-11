using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Modules.OT.Application.Queries;
using Flit.Modules.OT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.OT.Tests.Labels;

public class GetOtLabelImpactQueryHandlerTests
{
    private readonly IOtOrganismRepository _organismRepo = Substitute.For<IOtOrganismRepository>();
    private readonly IOtDocumentLabelRepository _labelRepo = Substitute.For<IOtDocumentLabelRepository>();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtId = Guid.NewGuid();
    private static readonly Guid LabelId = Guid.NewGuid();

    private readonly GetOtLabelImpactQueryHandler _sut;

    public GetOtLabelImpactQueryHandlerTests()
    {
        _sut = new GetOtLabelImpactQueryHandler(_organismRepo, _labelRepo);
    }

    [Fact]
    public async Task GetImpact_RetornaConteoDeAdjuntos()
    {
        _organismRepo.FindByIdAsync(OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtOrganism { Id = OtId, TenantId = TenantId });
        _labelRepo.FindByIdAsync(LabelId, OtId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new OtDocumentLabel
            {
                Id = LabelId,
                OtId = OtId,
                TenantId = TenantId,
                Slug = "licencia_transito"
            });
        _labelRepo.CountAttachmentImpactAsync(OtId, "licencia_transito", TenantId, Arg.Any<CancellationToken>())
            .Returns(15);

        var result = await _sut.HandleAsync(new GetOtLabelImpactQuery(OtId, LabelId, TenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value.ImpactCount.Should().Be(15);
    }
}
