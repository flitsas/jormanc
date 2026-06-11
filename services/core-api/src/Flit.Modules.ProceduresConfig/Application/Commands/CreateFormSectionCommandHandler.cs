using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Application.Mapping;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed class CreateFormSectionCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<FormSectionDto, ProcedureTypeError>> HandleAsync(
        CreateFormSectionCommand command, CancellationToken ct = default)
    {
        var step = await repository.FindStepAsync(command.ProcedureTypeId, command.StepId, command.TenantId, ct);
        if (step is null)
            return Result<FormSectionDto, ProcedureTypeError>.Failure(ProcedureTypeError.StepNotFound);

        var now = clock.UtcNow;
        var section = new FormSection
        {
            Id = Guid.NewGuid(),
            StepId = command.StepId,
            TenantId = command.TenantId,
            OrderIndex = command.OrderIndex,
            Slug = command.Slug.Trim().ToLowerInvariant(),
            Name = command.Name.Trim(),
            IsCollapsible = command.IsCollapsible,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddSectionAsync(section, ct);
        return Result<FormSectionDto, ProcedureTypeError>.Success(ProcedureTypeMapper.ToSectionDto(section));
    }
}
