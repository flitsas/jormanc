using Flit.Modules.Integrations.Application.DTOs;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.Modules.Integrations.Infrastructure;
using Flit.SharedKernel;

namespace Flit.Modules.Integrations.Application.UseCases;

/// <summary>
/// Caso de uso: consulta vehículo por placa usando el proveedor RUNT configurado por tenant.
/// AC1 — usa proveedor primario configurado en connector_configs.
/// AC2 — el ConnectorRouter aplica failover automático si el primario falla (timeout/5xx).
/// AC3 — cada llamada registra payload en integration_logs por tenant.
/// HU-9775.
/// </summary>
public sealed class QueryVehicleByPlateUseCase(ConnectorRouter connectorRouter)
{
    public async Task<Result<VehicleQueryResultDto, IntegrationsError>> ExecuteAsync(
        Guid tenantId,
        string plate,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            return Result<VehicleQueryResultDto, IntegrationsError>.Failure(
                IntegrationsError.TenantRequired);

        if (string.IsNullOrWhiteSpace(plate))
            return Result<VehicleQueryResultDto, IntegrationsError>.Failure(
                IntegrationsError.ConnectorConfigNotFound);

        try
        {
            var (result, provider, durationMs) = await connectorRouter
                .ExecuteRuntAsync(tenantId, (c, innerCt) => c.QueryVehicleByPlateAsync(plate, innerCt),
                    "runt.vehicle.plate", ct);

            return Result<VehicleQueryResultDto, IntegrationsError>.Success(
                new VehicleQueryResultDto(
                    Found: result.Found,
                    Plate: result.Plate,
                    Vin: result.Vin,
                    Brand: result.Brand,
                    Model: result.Model,
                    Year: result.Year,
                    Color: result.Color,
                    FuelType: result.FuelType,
                    OwnerDocument: result.OwnerDocument,
                    OwnerName: result.OwnerName,
                    Status: result.Status,
                    Restrictions: result.Restrictions,
                    Provider: provider,
                    DurationMs: durationMs));
        }
        catch (AllConnectorsFailedException)
        {
            return Result<VehicleQueryResultDto, IntegrationsError>.Failure(
                IntegrationsError.AllConnectorsFailed);
        }
    }
}
