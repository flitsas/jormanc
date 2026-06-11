using System.Diagnostics;
using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Modules.Integrations.Domain.Errors;
using Flit.Modules.Integrations.Domain.Interfaces;
using Flit.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Flit.Modules.Integrations.Infrastructure;

/// <summary>
/// Orquestador de conectores RUNT con hot-failover automático (ADR-0011 Opción 2).
///
/// Flujo por operación:
///   1. Carga connector_configs activos del tenant, ordenados por priority ASC.
///   2. Para cada conector, ejecuta la operación con un CancellationToken que expira
///      en config.TimeoutMs (default 4000ms).
///   3. Si timeout (OperationCanceledException) o HTTP 5xx (ConnectorServerException)
///      → registra log con status correspondiente y pasa al siguiente.
///   4. Si la operación es exitosa → registra log success y retorna el resultado.
///   5. Si todos los conectores fallan → lanza AllConnectorsFailedException.
///
/// Recibe IEnumerable&lt;IRuntConnector&gt; con todas las implementaciones registradas.
/// Resuelve el conector correcto en runtime leyendo connector_configs por provider name.
/// AC2 HU-9775: failover automático tras timeout >4s o HTTP 5xx.
/// AC3 HU-9775: cada llamada genera un IntegrationLog por tenant.
/// </summary>
public sealed class ConnectorRouter(
    IEnumerable<IRuntConnector> runtConnectors,
    IConnectorConfigRepository configRepository,
    IIntegrationLogRepository logRepository,
    IClock clock,
    ILogger<ConnectorRouter> logger)
{
    private const string ConnectorType = "runt";
    private readonly Dictionary<string, IRuntConnector> _connectorMap =
        runtConnectors.ToDictionary(c => c.ProviderName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ejecuta una operación RUNT con failover automático según la configuración del tenant.
    /// La lambda recibe (conector, cancellationToken) donde el CT incluye el timeout del config.
    /// Retorna (resultado, nombreProveedor, duracionMs).
    /// </summary>
    public async Task<(TResult Result, string Provider, int DurationMs)> ExecuteRuntAsync<TResult>(
        Guid tenantId,
        Func<IRuntConnector, CancellationToken, Task<TResult>> operation,
        string operationName,
        CancellationToken externalCt = default)
    {
        var chain = await BuildChainAsync(tenantId, externalCt);

        if (chain.Count == 0)
        {
            logger.LogWarning(
                "Tenant {TenantId} no tiene connector_configs para 'runt'. Usando mock.", tenantId);
            chain = [(Resolve("mock"), 4000)];
        }

        int attemptCount = 0;

        foreach (var (connector, timeoutMs) in chain)
        {
            attemptCount++;
            var sw = Stopwatch.StartNew();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            cts.CancelAfter(timeoutMs);

            string? requestPayloadJson = null;
            string? responsePayloadJson = null;
            int? httpStatus = null;
            string? errorMessage = null;

            try
            {
                var result = await operation(connector, cts.Token);
                sw.Stop();

                responsePayloadJson = SerializeSafe(result);
                await PersistLogAsync(tenantId, connector.ProviderName, operationName,
                    requestPayloadJson, responsePayloadJson, 200, (int)sw.ElapsedMilliseconds,
                    null, externalCt);

                logger.LogInformation(
                    "RUNT {Op} exitoso vía {Provider} para tenant {TenantId} en {DurationMs}ms",
                    operationName, connector.ProviderName, tenantId, sw.ElapsedMilliseconds);

                return (result, connector.ProviderName, (int)sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (!externalCt.IsCancellationRequested)
            {
                sw.Stop();
                errorMessage = $"Timeout tras {timeoutMs}ms";
                httpStatus = 408;

                logger.LogWarning(
                    "RUNT {Op} timeout en proveedor {Provider} (tenant {TenantId}). Intentando failover.",
                    operationName, connector.ProviderName, tenantId);
            }
            catch (ConnectorServerException ex)
            {
                sw.Stop();
                errorMessage = ex.Message;
                httpStatus = ex.HttpStatus;

                logger.LogWarning(
                    "RUNT {Op} HTTP {Status} en proveedor {Provider} (tenant {TenantId}). Intentando failover.",
                    operationName, ex.HttpStatus, connector.ProviderName, tenantId);
            }
            finally
            {
                if (sw.IsRunning) sw.Stop();
                if (errorMessage is not null)
                {
                    await PersistLogAsync(tenantId, connector.ProviderName, operationName,
                        requestPayloadJson, responsePayloadJson, httpStatus,
                        (int)sw.ElapsedMilliseconds, errorMessage, externalCt);
                }
            }
        }

        throw new AllConnectorsFailedException(operationName, attemptCount);
    }

    // ─── Chain builder ───────────────────────────────────────────────────────

    private async Task<List<(IRuntConnector Connector, int TimeoutMs)>> BuildChainAsync(
        Guid tenantId, CancellationToken ct)
    {
        var configs = await configRepository.GetActiveByTenantAndTypeAsync(
            tenantId, ConnectorType, ct);

        return configs
            .OrderBy(c => c.Priority)
            .Select(c => (Resolve(c.Provider), c.TimeoutMs))
            .ToList();
    }

    private IRuntConnector Resolve(string provider) =>
        _connectorMap.TryGetValue(provider, out var connector)
            ? connector
            : _connectorMap.TryGetValue("mock", out var fallback) ? fallback
            : throw new InvalidOperationException($"No hay conector registrado para provider '{provider}'.");

    // ─── Log persistence (best-effort) ───────────────────────────────────────

    private async Task PersistLogAsync(
        Guid tenantId, string provider, string operation,
        string? requestPayload, string? responsePayload,
        int? httpStatus, int durationMs, string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            var log = new IntegrationLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConnectorType = ConnectorType,
                Operation = operation,
                Provider = provider,
                RequestPayload = requestPayload,
                ResponsePayload = responsePayload,
                HttpStatus = httpStatus,
                DurationMs = durationMs,
                ErrorMessage = errorMessage,
                LoggedAt = clock.UtcNow
            };
            await logRepository.AddAsync(log, ct);
        }
        catch (Exception ex)
        {
            // logging es best-effort — no lanzar excepción si falla la persistencia del log
            logger.LogWarning(ex, "No se pudo persistir IntegrationLog para tenant {TenantId}", tenantId);
        }
    }

    private static string? SerializeSafe(object? obj)
    {
        if (obj is null) return null;
        try { return JsonSerializer.Serialize(obj); }
        catch { return null; }
    }
}
