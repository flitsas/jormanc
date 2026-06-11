using System.Text.Json;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.Modules.ProceduresConfig.Domain.Models;
using Flit.Modules.ProceduresConfig.Domain.Services;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Queries;

/// <summary>AC3 HU-9780 — dry-run simulación sin persistir</summary>
public sealed class SimulateCoherenceQueryHandler(
    IProcedureTypeRepository repository,
    ICoherenceSimulator coherenceSimulator)
{
    public async Task<Result<CoherenceSimulationDto, ProcedureTypeError>> HandleAsync(
        SimulateCoherenceQuery query, CancellationToken ct = default)
    {
        try
        {
            JsonDocument.Parse(query.ConditionsJson);
            JsonDocument.Parse(query.ActionsJson);
        }
        catch
        {
            return Result<CoherenceSimulationDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidRuleJson);
        }

        var procedureType = await repository.FindByIdAsync(query.ProcedureTypeId, query.TenantId, ct);
        if (procedureType is null)
            return Result<CoherenceSimulationDto, ProcedureTypeError>.Failure(ProcedureTypeError.NotFound);

        var existing = await repository.GetRuleSetsAsync(query.ProcedureTypeId, query.TenantId, ct);
        var candidate = new RuleDefinition(null, query.Name, query.ConditionsJson, query.ActionsJson);
        var existingDefs = existing.Select(r => new RuleDefinition(r.Id, r.Name, r.Conditions, r.Actions)).ToList();

        var simulation = coherenceSimulator.Simulate(existingDefs, candidate);

        return Result<CoherenceSimulationDto, ProcedureTypeError>.Success(new CoherenceSimulationDto(
            IsCoherent: simulation.IsCoherent,
            Conflicts: simulation.Conflicts
                .Select(c => new CoherenceConflictDto(c.Rule1, c.Rule2, c.Description))
                .ToList()));
    }
}
