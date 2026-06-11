namespace Flit.Modules.Documents.Application.Commands;

public sealed record AssociateDocumentToProcedureTypeCommand(
    Guid ProcedureTypeId,
    Guid TenantId,
    Guid RequestedByUserId,
    Guid DocumentTypeId,
    bool IsRequired,
    int OrderIndex,
    Guid? ActorDefinitionId,
    bool AllowPartialConsolidation);
