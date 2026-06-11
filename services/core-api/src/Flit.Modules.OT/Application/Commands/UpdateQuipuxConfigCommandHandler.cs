using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flit.Modules.OT.Application.DTOs;
using Flit.Modules.OT.Domain.Errors;
using Flit.Modules.OT.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.OT.Application.Commands;

/// <summary>PUT /api/v1/ot-organisms/{id}/quipux-config — HU-9800/9801</summary>
public sealed class UpdateQuipuxConfigCommandHandler(
    IOtOrganismRepository repository,
    IClock clock)
{
    public async Task<Result<OtOrganismDto, OtError>> HandleAsync(
        UpdateQuipuxConfigCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Endpoint))
            return Result<OtOrganismDto, OtError>.Failure(
                new OtError("VALIDATION_ERROR", "endpoint es requerido."));

        var organism = await repository.FindByIdAsync(command.Id, command.TenantId, ct);
        if (organism is null)
            return Result<OtOrganismDto, OtError>.Failure(OtError.NotFound);

        var tokenHash = ResolveTokenHash(command.WebhookToken, organism.QuipuxConfig);
        if (tokenHash is null)
            return Result<OtOrganismDto, OtError>.Failure(
                new OtError("VALIDATION_ERROR", "webhookToken es requerido en la configuración inicial."));

        organism.QuipuxConfig = JsonSerializer.Serialize(new
        {
            endpoint = command.Endpoint.Trim(),
            webhook_token_hash = tokenHash
        });
        organism.UpdatedAt = clock.UtcNow;
        organism.UpdatedBy = command.RequestedByUserId;

        await repository.UpdateAsync(organism, ct);

        return Result<OtOrganismDto, OtError>.Success(OtOrganismMapper.ToDto(organism));
    }

    private static string? ResolveTokenHash(string? newToken, string? existingConfigJson)
    {
        if (!string.IsNullOrWhiteSpace(newToken))
            return HashToken(newToken.Trim());

        if (string.IsNullOrWhiteSpace(existingConfigJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(existingConfigJson);
            if (doc.RootElement.TryGetProperty("webhook_token_hash", out var hashProp))
                return hashProp.GetString();
        }
        catch (JsonException)
        {
            // ignored
        }

        return null;
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
