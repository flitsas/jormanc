using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Queries;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

/// <summary>AC1 HU-9779 — GET estructura anidada</summary>
public class GetProcedureTypeQueryHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly GetProcedureTypeQueryHandler _sut;

    public GetProcedureTypeQueryHandlerTests() => _sut = new GetProcedureTypeQueryHandler(_repo);

    [Fact]
    public async Task AC1_GetById_RetornaStepsSectionsFieldsAnidados()
    {
        var tenantId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        var entity = new ProcedureType
        {
            Id = typeId,
            TenantId = tenantId,
            Slug = "traspaso-simple",
            Name = "Traspaso Simple",
            Family = "traspasos",
            Scope = "global",
            VehicleQueryKey = "placa",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            Steps =
            [
                new ProcedureStep
                {
                    Id = stepId,
                    ProcedureTypeId = typeId,
                    TenantId = tenantId,
                    OrderIndex = 1,
                    Name = "Datos del vehículo",
                    StepType = "form",
                    FormSections =
                    [
                        new FormSection
                        {
                            Id = sectionId,
                            StepId = stepId,
                            TenantId = tenantId,
                            Slug = "vehiculo",
                            Name = "Vehículo",
                            OrderIndex = 1,
                            FormFields =
                            [
                                new FormField
                                {
                                    Id = Guid.NewGuid(),
                                    SectionId = sectionId,
                                    TenantId = tenantId,
                                    Slug = "placa",
                                    Name = "Placa",
                                    FieldType = "text",
                                    IsRequired = true,
                                    Config = "{}",
                                    OrderIndex = 1
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        _repo.FindByIdWithDetailsAsync(typeId, tenantId, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.HandleAsync(new GetProcedureTypeQuery(typeId, tenantId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Steps.Should().HaveCount(1);
        result.Value.Steps[0].Sections.Should().HaveCount(1);
        result.Value.Steps[0].Sections[0].Fields.Should().HaveCount(1);
        result.Value.Steps[0].Sections[0].Fields[0].Slug.Should().Be("placa");
    }
}
