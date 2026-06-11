using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Modules.Documents.Application.DTOs;
using Flit.Modules.Documents.Domain.Errors;
using Flit.Modules.Documents.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Documents.Application.Commands;

/// <summary>AC1 HU-9789 — POST /document-types</summary>
public sealed class CreateDocumentTypeCommandHandler(
    IDocumentRepository repository,
    IClock clock)
{
    private static readonly HashSet<string> ValidLoadTypes = ["carga", "generacion"];

    public async Task<Result<DocumentTypeDto, DocumentError>> HandleAsync(
        CreateDocumentTypeCommand command, CancellationToken ct = default)
    {
        var loadType = command.LoadType.Trim().ToLowerInvariant();
        if (!ValidLoadTypes.Contains(loadType))
            return Result<DocumentTypeDto, DocumentError>.Failure(DocumentError.InvalidLoadType);

        var now = clock.UtcNow;
        var entity = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.Name.Trim(),
            LoadType = loadType,
            AllowedFormats = string.IsNullOrWhiteSpace(command.AllowedFormats)
                ? """["pdf"]"""
                : command.AllowedFormats,
            MaxSizeMb = command.MaxSizeMb is > 0 ? command.MaxSizeMb.Value : 10,
            IsReusable = command.IsReusable ?? true,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.CreateDocumentTypeAsync(entity, ct);

        return Result<DocumentTypeDto, DocumentError>.Success(new DocumentTypeDto(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.LoadType,
            entity.AllowedFormats,
            entity.MaxSizeMb,
            entity.IsReusable,
            entity.CreatedAt));
    }
}
