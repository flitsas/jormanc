using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.Commands;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;
using NSubstitute;
using Xunit;

namespace Flit.ProceduresConfig.Tests.ProcedureTypes;

public class UpdateFormFieldCommandHandlerTests
{
    private readonly IProcedureTypeRepository _repo = Substitute.For<IProcedureTypeRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly UpdateFormFieldCommandHandler _sut;

    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();
    private static readonly Guid SectionId = Guid.NewGuid();
    private static readonly Guid FieldId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private const string DropdownConfig = """{"options":[{"value":"cc","label":"Cédula"}]}""";

    public UpdateFormFieldCommandHandlerTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _sut = new UpdateFormFieldCommandHandler(_repo, _clock);
    }

    [Fact]
    public async Task AC2_ActualizaCampoDropdownConConfig()
    {
        _repo.FindSectionAsync(StepId, SectionId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FormSection { Id = SectionId, StepId = StepId });
        _repo.FindFieldAsync(SectionId, FieldId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new FormField
            {
                Id = FieldId,
                SectionId = SectionId,
                FieldType = "text",
                Config = "{}"
            });

        var command = new UpdateFormFieldCommand(
            TypeId, StepId, SectionId, FieldId, TenantId, UserId,
            "Tipo doc", "dropdown", true, DropdownConfig);
        var result = await _sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.FieldType.Should().Be("dropdown");
        result.Value.Config.Should().Be(DropdownConfig);
    }
}
