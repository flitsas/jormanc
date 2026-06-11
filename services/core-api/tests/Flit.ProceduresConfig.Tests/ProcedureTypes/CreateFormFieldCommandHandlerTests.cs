using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

/// <summary>AC2 HU-9779 — campo dropdown con config JSONB</summary>
public class CreateFormFieldCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly CreateFormFieldCommandHandler _sut;

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 11, 14, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcedureTypeId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();
    private static readonly Guid SectionId = Guid.NewGuid();

    public CreateFormFieldCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _sut = new CreateFormFieldCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC2_Dropdown_PersisteConfigJsonb()
    {
        _repo.FindSectionAsync(StepId, SectionId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FormSection { Id = SectionId, StepId = StepId, TenantId = TenantId });

        var configJson =
            """{"options":[{"value":"A","label":"Opción A"}],"allow_multiple":false}""";

        var command = new CreateFormFieldCommand(
            ProcedureTypeId, StepId, SectionId, TenantId, UserId,
            "tipo_doc", "Tipo documento", "dropdown", true, configJson, 1);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.FieldType.Should().Be("dropdown");
        result.Value.Config.Should().Contain("Opción A");

        await _repo.Received(1).AddFieldAsync(
            Arg.Is<FormField>(f => f.FieldType == "dropdown" && f.Config.Contains("allow_multiple")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AC2_ConfigInvalida_RetornaError()
    {
        _repo.FindSectionAsync(StepId, SectionId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FormSection { Id = SectionId });

        var command = new CreateFormFieldCommand(
            ProcedureTypeId, StepId, SectionId, TenantId, UserId,
            "campo", "Campo", "text", false, "not-json", 1);

        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("FORM_FIELD_INVALID_CONFIG");
    }
}
