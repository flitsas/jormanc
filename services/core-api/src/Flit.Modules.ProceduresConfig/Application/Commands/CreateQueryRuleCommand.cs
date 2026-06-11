namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record CreateQueryRuleCommand(
    Guid ProcedureTypeId,
    Guid ActorId,
    Guid TenantId,
    Guid RequestedByUserId,
    string SubjectType,
    string EntryKey,
    bool IsBlocking,
    string VerificationsJson);
