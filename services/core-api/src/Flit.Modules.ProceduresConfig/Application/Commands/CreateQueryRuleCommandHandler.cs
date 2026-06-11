using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Flit.Modules.ProceduresConfig.Application.DTOs;
using Flit.Modules.ProceduresConfig.Domain.Errors;
using Flit.Modules.ProceduresConfig.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.ProceduresConfig.Application.Commands;

/// <summary>AC2 HU-9781 — POST query-rules con verifications JSONB</summary>
public sealed class CreateQueryRuleCommandHandler(
    IProcedureTypeRepository repository,
    IClock clock)
{
    public async Task<Result<QueryRuleDto, ProcedureTypeError>> HandleAsync(
        CreateQueryRuleCommand command, CancellationToken ct = default)
    {
        if (!IsValidVerificationsArray(command.VerificationsJson))
            return Result<QueryRuleDto, ProcedureTypeError>.Failure(ProcedureTypeError.InvalidVerificationsJson);

        var actor = await repository.FindActorAsync(command.ProcedureTypeId, command.ActorId, command.TenantId, ct);
        if (actor is null)
            return Result<QueryRuleDto, ProcedureTypeError>.Failure(ProcedureTypeError.ActorNotFound);

        var now = clock.UtcNow;
        var queryRule = new QueryRule
        {
            Id = Guid.NewGuid(),
            ActorDefinitionId = command.ActorId,
            TenantId = command.TenantId,
            SubjectType = command.SubjectType.Trim().ToLowerInvariant(),
            EntryKey = command.EntryKey.Trim().ToLowerInvariant(),
            IsBlocking = command.IsBlocking,
            Verifications = command.VerificationsJson,
            CreatedAt = now,
            CreatedBy = command.RequestedByUserId,
            UpdatedAt = now,
            UpdatedBy = command.RequestedByUserId
        };

        await repository.AddQueryRuleAsync(queryRule, ct);

        return Result<QueryRuleDto, ProcedureTypeError>.Success(Map(queryRule));
    }

    internal static bool IsValidVerificationsArray(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch
        {
            return false;
        }
    }

    internal static QueryRuleDto Map(QueryRule q) =>
        new(q.Id, q.ActorDefinitionId, q.TenantId, q.SubjectType, q.EntryKey,
            q.IsBlocking, q.Verifications, q.CreatedAt);
}
