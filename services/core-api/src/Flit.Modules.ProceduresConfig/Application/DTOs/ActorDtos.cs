namespace Flit.Modules.ProceduresConfig.Application.DTOs;

public sealed record ActorDefinitionDto(
    Guid Id,
    Guid ProcedureTypeId,
    Guid TenantId,
    string Role,
    string AllowedNature,
    int MinCount,
    int MaxCount,
    bool IsRequired,
    int OrderIndex,
    Guid? LegalRepActorId,
    DateTimeOffset CreatedAt);

public sealed record QueryRuleDto(
    Guid Id,
    Guid ActorDefinitionId,
    Guid TenantId,
    string SubjectType,
    string EntryKey,
    bool IsBlocking,
    string Verifications,
    DateTimeOffset CreatedAt);
