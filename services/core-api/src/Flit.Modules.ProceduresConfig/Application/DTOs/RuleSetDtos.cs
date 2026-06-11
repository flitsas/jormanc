namespace Flit.Modules.ProceduresConfig.Application.DTOs;

public sealed record RuleSetDto(
    Guid Id,
    Guid ProcedureTypeId,
    Guid TenantId,
    string Name,
    string Conditions,
    string Actions,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CoherenceSimulationDto(
    bool IsCoherent,
    IReadOnlyList<CoherenceConflictDto> Conflicts);

public sealed record CoherenceConflictDto(string Rule1, string Rule2, string Description);
