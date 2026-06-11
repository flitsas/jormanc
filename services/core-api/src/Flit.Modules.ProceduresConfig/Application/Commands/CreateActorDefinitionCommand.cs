namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateActorDefinitionCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    string Role,
    string AllowedNature,
    int MinCount,
    int MaxCount,
    bool IsRequired,
    int OrderIndex,
    Guid? LegalRepActorId);
