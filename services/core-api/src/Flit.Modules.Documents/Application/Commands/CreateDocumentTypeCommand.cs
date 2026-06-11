namespace Flit.Modules.Documents.Application.Commands;

public sealed record CreateDocumentTypeCommand(
    Guid TenantId,
    Guid RequestedByUserId,
    string Name,
    string LoadType,
    string? AllowedFormats,
    int? MaxSizeMb,
    bool? IsReusable);
