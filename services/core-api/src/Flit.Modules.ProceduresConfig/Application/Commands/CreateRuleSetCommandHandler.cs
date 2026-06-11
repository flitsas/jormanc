using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Models;
using Flit.Modules.ProceduresConfig.Domain.Services;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC1/AC2 HU-9780 — POST /procedure-types/{id}/rules</summary>
public sealed class CreateRuleSetCommandHandler(
    IProcedureTypeRepository repository,
    ICoherenceSimulator coherenceSimulator,
    IClock clock)
{
    public async Task<Result<RuleSetDto, ProcedureTypeError>> HandleAsync(
        CreateRuleSetCommand command, CancellationToken ct = default)
    {
        if (!TryParseJson(command.ConditionsJson) || !TryParseJson(command.ActionsJson))
            return Result<RuleSetDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidRuleJson);

        var procedureType = await repository.FindByIdAsync(command.ProcedureTypeId, command.TenantId, ct);
        if (procedureType is null)
            return Result<RuleSetDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        var existing = await repository.GetRuleSetsAsync(command.ProcedureTypeId, command.TenantId, ct);
        var candidate = new RuleDefinition(null, command.Name, command.ConditionsJson, command.ActionsJson);
        var existingDefs = existing.Select(r => new RuleDefinition(r.Id, r.Name, r.Conditions, r.Actions)).ToList();

        var simulation = coherenceSimulator.Simulate(existingDefs, candidate);
        if (!simulation.IsCoherent)
        {
            var conflicts = simulation.Conflicts
                .Select(c => new RuleConflictDetail(c.Rule1, c.Rule2, c.Description))
                .ToList();
            return Result<RuleSetDto, ProcedureTypeError>.Failure(ProcedureTypeError.RuleConflict(conflicts));
        }

        var now = clock.UtcNow;
        var ruleSet = new RuleSet
        {
            Id = Guid.NewGuid(),
            ProcedureTypeId = command.ProcedureTypeId,
            TenantId = command.TenantId,
            Name = command.Name.Trim(),
            Conditions = command.ConditionsJson,
            Actions = command.ActionsJson,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddRuleSetAndIncrementVersionAsync(ruleSet, procedureType, ct);

        return Result<RuleSetDto, ProcedureTypeError>.Success(new RuleSetDto(
            Id: ruleSet.Id,
            ProcedureTypeId: ruleSet.ProcedureTypeId,
            TenantId: ruleSet.TenantId,
            Name: ruleSet.Name,
            Conditions: ruleSet.Conditions,
            Actions: ruleSet.Actions,
            IsActive: ruleSet.IsActive,
            CreatedAt: ruleSet.CreatedAt));
    }

    private static bool TryParseJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
