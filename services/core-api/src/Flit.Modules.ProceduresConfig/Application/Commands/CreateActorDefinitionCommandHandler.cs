using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC1 HU-9781 — POST /procedure-types/{id}/actors</summary>
public sealed class CreateActorDefinitionCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<ActorDefinitionDto, ProcedureTypeError>> HandleAsync(
        CreateActorDefinitionCommand command, CancellationToken ct = default)
    {
        var procedureType = await repository.FindByIdAsync(command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<ActorDefinitionDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        if (command.LegalRepActorId.HasValue)
        {
            var legalRep = await repository.FindActorAsync(
                command.ProcedureTypeId, command.LegalRepActorId.Value, command.TenantId, ct);
            if (legalRep is null)
                return Result<ActorDefinitionDto, ProcedureTypeError>.Failure(ProcedureTypeError.LegalRepActorNotFound);
        }

        var now = clock.UtcNow;
        var actor = new ActorDefinition
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = command.ProcedureTypeId,
            TenantId = command.TenantId,
            Role = command.Role.Trim().ToLowerInvariant(),
            AllowedNature = command.AllowedNature.Trim().ToLowerInvariant(),
            MinCount = command.MinCount,
            MaxCount = command.MaxCount,
            IsRequired = command.IsRequired,
            OrderIndex = command.OrderIndex,
            LegalRepActorId = command.LegalRepActorId,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddActorAsync(actor, ct);

        return Result<ActorDefinitionDto, ProcedureTypeError>.Success(Map(actor));
    }

    internal static ActorDefinitionDto Map(ActorDefinition a) =>
        new(a.Id, a.ProcedureTypeId, a.TenantId, a.Role, a.AllowedNature,
            a.MinCount, a.MaxCount, a.IsRequired, a.OrderIndex, a.LegalRepActorId, a.CreatedAt);
}
