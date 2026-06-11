using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Application.DTOs;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Integrations.Application.Commands;

/// <summary>
/// Handler de UpsertConnectorConfigCommand.
/// Valida proveedor y tipo, luego persiste o actualiza el registro en connector_configs.
/// AC1 HU-9775: configura el proveedor RUNT primario por tenant.
/// </summary>
public sealed class UpsertConnectorConfigCommandHandler(
    IConnectorConfigRepository configRepository,
    IClock clock)
{
    private static readonly HashSet<string> ValidProviders =
        ["verifik", "intempo", "mock"];

    private static readonly HashSet<string> ValidConnectorTypes =
        ["runt", "simit", "rues", "identity", "quipux"];

    public async Task<Result<ConnectorConfigDto, IntegrationsError>> HandleAsync(
        UpsertConnectorConfigCommand command, CancellationToken ct = default)
    {
        if (!ValidProviders.Contains(command.Provider))
            return Result<ConnectorConfigDto, IntegrationsError>.Failure(
                IntegrationsError.InvalidProvider);

        if (!ValidConnectorTypes.Contains(command.ConnectorType))
            return Result<ConnectorConfigDto, IntegrationsError>.Failure(
                IntegrationsError.InvalidConnectorType);

        var now = clock.UtcNow;

        var existing = (await configRepository.GetActiveByTenantAndTypeAsync(
            command.TenantId, command.ConnectorType, ct))
            .FirstOrDefault(c => c.Provider == command.Provider);

        ConnectorConfig config;

        if (existing is not null)
        {
            existing.IsPrimary = command.IsPrimary;
            existing.Priority = command.Priority;
            existing.TimeoutMs = command.TimeoutMs;
            existing.IsActive = command.IsActive;
            existing.UpdatedAt = now;
            existing.UpdatedBy = command.RequestedByUserId;
            config = existing;
        }
        else
        {
            config = new ConnectorConfig
            {
                Id = Guid.NewGuid(),
                TenantId = command.TenantId,
                ConnectorType = command.ConnectorType,
                Provider = command.Provider,
                CredentialsRef = "{}",
                IsPrimary = command.IsPrimary,
                Priority = command.Priority,
                TimeoutMs = command.TimeoutMs,
                IsActive = command.IsActive,
                CreatedAt = now,
                CreatedBy = command.RequestedByUserId,
                UpdatedAt = now,
                UpdatedBy = command.RequestedByUserId,
                RowVersion = 1
            };
        }

        await configRepository.UpsertAsync(config, ct);

        return Result<ConnectorConfigDto, IntegrationsError>.Success(
            MapToDto(config));
    }

    private static ConnectorConfigDto MapToDto(ConnectorConfig c) =>
        new(c.Id, c.TenantId, c.ConnectorType, c.Provider,
            c.IsPrimary, c.Priority, c.TimeoutMs, c.IsActive,
            c.CreatedAt, c.UpdatedAt);
}
