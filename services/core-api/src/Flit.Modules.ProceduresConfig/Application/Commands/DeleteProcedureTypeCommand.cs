namespace Flit.Modules.ProceduresConfig.Application.Commands;

public sealed record DeleteProcedureTypeCommand(Guid Id, Guid TenantId, Guid RequestedByUserId);
