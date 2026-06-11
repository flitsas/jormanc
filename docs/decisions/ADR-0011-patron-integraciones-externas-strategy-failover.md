# ADR-0011: Patrón de Integraciones Externas — Strategy + Hot-Failover Automático

**Fecha:** 2026-06-10
**Status:** Propuesto
**Deciders:** Líder Técnico FLIT
**Autor:** Architecture Agent (architecture-agent v2.0)
**Tags:** arquitectura, backend, integraciones, strategy, failover, runt, verifik, intempo, simit, rues
**Relaciona:** ADR-0004 (consolidación stack), ADR-0007 (YARP gateway)
**Features ADO:** #9565 (proxy RUNT), #9731 (consultas background Verifik/RUNT), #9566 (Quipux)

---

## Contexto

FLIT 2.0 debe integrarse con múltiples sistemas externos del Estado colombiano y proveedores privados:
- **RUNT** (vía Verifik como proveedor primario, Intempo como secundario): consultas de vehículos, personas, restricciones, licencias.
- **SIMIT**: verificación de multas pendientes.
- **RUES**: existencia y representante legal de personas jurídicas.
- **Verifik**: liveness biométrico (validación de identidad).
- **Quipux**: integración documental OT (webhooks/callbacks).

El Feature #9565 exige un **proxy RUNT con patrón Strategy** (Verifik/Intempo) con **hot-failover automático** (timeout >4s o respuesta 5xx) y logs de payloads por tenant. El Feature #9731 requiere que las consultas se ejecuten en **background tras capturar placa**, con resultados almacenados.

### Restricciones

| # | Restricción |
|---|---|
| C1 | Ninguna integración puede estar hardcoded — todo configurable por tenant (ConnectorConfig) |
| C2 | Hot-failover en <1s entre Verifik e Intempo cuando el primario falla |
| C3 | Logs completos de payload (request + response) por tenant — auditoría y debugging |
| C4 | Implementaciones mock intercambiables para DEV/TEST sin modificar código de dominio |
| C5 | `.env.verifik` contiene credenciales reales de Verifik — solo para ambiente real |
| C6 | Las consultas de RUNT/SIMIT son síncronas desde la perspectiva del usuario (bloquean paso del stepper o son informativas) |
| C7 | Liveness (identidad) es una operación larga (~5-15s) — debe ser asíncrona con feedback SignalR |

---

## Decisión

Adoptar el **patrón Strategy con interfaces de dominio** + **ConnectorRouter para hot-failover automático** + **Wolverine para operaciones asíncronas largas** + **registro inmutable de IntegrationLog por operación**.

---

## Alternativas consideradas

### Opción 1: HTTP directo en handlers — sin abstracción

**Descripción:** Los Wolverine handlers (ej. `VehicleQueryHandler`) llaman directamente a `HttpClient` configurado con la URL de Verifik. No hay interfaz de por medio.

**Pros:**
- Mínimo código: un `HttpClient` named y listo.
- Sin abstracciones innecesarias para un proveedor único.
- Curva de aprendizaje cero.

**Cons:**
- Cambio de proveedor requiere modificar el handler (violación Open/Closed Principle).
- Imposible intercambiar por mock en tests sin modificar código.
- Failover a Intempo requiere lógica ad-hoc en el handler → código duplicado si hay múltiples consultas.
- Logs de payload deben implementarse manualmente en cada handler.
- Viola C1 (configurable por tenant) y C4 (mocks intercambiables).

**Esfuerzo estimado:** S (inicial) / L (cuando se agrega failover y mocks)
**Riesgos principales:** Deuda técnica inmediata; cambio de proveedor RUNT → modificar múltiples handlers.

---

### Opción 2 (recomendada): Strategy pattern + interfaces de dominio + ConnectorRouter + hot-failover

**Descripción:** Cada sistema externo tiene una **interfaz de dominio** en `Flit.Modules.Integrations/Domain/Interfaces/`. Las implementaciones (Verifik, Intempo, Mock) implementan esa interfaz. Un `ConnectorRouter<T>` resuelve en tiempo de ejecución qué implementación usar basándose en `ConnectorConfig` del tenant, y aplica hot-failover automático con timeout configurable.

**Pros:**
- Intercambio de proveedor sin tocar el dominio (Open/Closed).
- Mocks intercambiables en DEV/TEST simplemente inyectando la implementación mock.
- `ConnectorRouter` centraliza la lógica de failover, retry y logging — un solo lugar de mantenimiento.
- `ConnectorConfig` por tenant permite Verifik como primario para unos tenants e Intempo para otros.
- `IntegrationLog` generado automáticamente por el router — auditoría completa sin código adicional en handlers.
- Preparado para agregar nuevos proveedores sin modificar interfaces ni handlers.

**Cons:**
- Más interfaces y archivos que la Opción 1.
- `ConnectorRouter` requiere implementación robusta con Polly o similar para timeouts/retry.
- La configuración dinámica por tenant requiere resolver el conector en runtime (no en DI startup).

**Esfuerzo estimado:** M
**Riesgos principales:** Configuración incorrecta de timeouts; el router puede enmascarar errores reales si el fallback siempre responde.

---

### Opción 3: Outbox + queue para todas las consultas externas

**Descripción:** Todas las integraciones externas se modelan como mensajes en cola (RabbitMQ via Wolverine). Los handlers publican `VehicleQueryRequested`, un consumer ejecuta la consulta externa y publica `VehicleQueryCompleted`. El frontend recibe el resultado por SignalR.

**Pros:**
- Máxima resiliencia: el sistema no depende de la disponibilidad de RUNT en el momento del request.
- Retry automático ante fallos transitorios sin bloquear al usuario.
- Desacoplamiento total entre consulta y respuesta.
- Funciona bien para operaciones largas (liveness ~15s).

**Cons:**
- Introduce latencia percibida: el usuario no ve el resultado inmediatamente.
- Complejidad significativa para consultas que se esperan "en tiempo real" (placa capturada → resultado visible en 1-2s).
- SignalR obligatorio para todas las integraciones, no solo las largas.
- Sobre-ingeniería para consultas rápidas (<2s) de SIMIT o RUES.
- Contraria a la UX del stepper donde ciertos resultados bloquean o informan antes de avanzar.

**Esfuerzo estimado:** L
**Riesgos principales:** UX degradada para consultas rápidas; complejidad operacional del sistema de mensajería.

---

## Tradeoff aceptado

Se elige la **Opción 2 (Strategy + ConnectorRouter)** con un matiz de la Opción 3 para operaciones largas:

- **Consultas síncronas rápidas** (<4s esperado: RUNT vehículo, SIMIT, RUES): Strategy + ConnectorRouter síncrono en el handler HTTP. El resultado se retorna directamente o se almacena en `VehicleQueries` y el frontend lo consume.
- **Operaciones asíncronas largas** (liveness biométrico ~5-15s): Wolverine + evento `LivenessValidationRequested` → consumer → `IntegrationLog` → `IdentityValidation` → SignalR push.

Esto combina el pragmatismo de la Opción 2 para el caso habitual con la resiliencia de la Opción 3 solo donde el tiempo de respuesta lo justifica.

---

## Diseño detallado

### Interfaces de dominio

```csharp
// Flit.Modules.Integrations/Domain/Interfaces/IRuntConnector.cs
public interface IRuntConnector
{
    Task<VehicleQueryResult> QueryVehicleByPlateAsync(string plate, CancellationToken ct);
    Task<VehicleQueryResult> QueryVehicleByVinAsync(string vin, CancellationToken ct);
    Task<PersonQueryResult> QueryPersonAsync(string documentNumber, CancellationToken ct);
    Task<RestrictionQueryResult> QueryRestrictionsAsync(string documentNumber, CancellationToken ct);
}

public interface ISimittConnector
{
    Task<FinesQueryResult> QueryFinesAsync(string documentNumber, CancellationToken ct);
}

public interface IRuesConnector
{
    Task<LegalEntityQueryResult> QueryLegalEntityAsync(string nit, CancellationToken ct);
}

public interface IIdentityValidationConnector
{
    Task<LivenessResult> ValidateLivenessAsync(ValidateLivenessRequest request, CancellationToken ct);
}

public interface IQuipuxConnector
{
    Task NotifyStatusChangeAsync(QuipuxStatusNotification notification, CancellationToken ct);
}
```

### ConnectorRouter con hot-failover

```csharp
// Flit.Modules.Integrations/Infrastructure/ConnectorRouter.cs
public sealed class ConnectorRouter<TConnector> : TConnector where TConnector : class
{
    private readonly IEnumerable<TConnector> _implementations;  // ordenadas por prioridad
    private readonly IIntegrationLogger _logger;
    private readonly ConnectorRouterOptions _options;           // timeout configurable

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<TConnector, Task<TResult>> operation,
        string operationName,
        CancellationToken ct)
    {
        foreach (var connector in _implementations)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_options.TimeoutMs);  // 4000ms por defecto

                var result = await operation(connector);
                await _logger.LogSuccessAsync(connector.GetType().Name, operationName, result);
                return result;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // timeout → intentar siguiente conector
                await _logger.LogTimeoutAsync(connector.GetType().Name, operationName);
            }
            catch (HttpRequestException ex) when (IsServerError(ex))
            {
                // 5xx → intentar siguiente conector
                await _logger.LogServerErrorAsync(connector.GetType().Name, operationName, ex);
            }
        }
        throw new AllConnectorsFailed(operationName);
    }
}
```

### Registro de IntegrationLog

```csharp
// Automático en el ConnectorRouter — cada operación genera un log
public sealed class IntegrationLog
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string ConnectorType { get; init; }    // "runt", "simit", "rues"
    public string Provider { get; init; }          // "verifik", "intempo", "mock"
    public string Operation { get; init; }         // "vehicle.plate", "person.id"
    public JsonDocument RequestPayload { get; init; }
    public JsonDocument ResponsePayload { get; init; }
    public int HttpStatus { get; init; }
    public int DurationMs { get; init; }
    public DateTimeOffset LoggedAt { get; init; }
}
```

### Estructura de implementaciones

```
Flit.Modules.Integrations/
└── Infrastructure/
    └── Connectors/
        ├── Runt/
        │   ├── VerifikRuntConnector.cs      ← IRuntConnector (primario)
        │   ├── IntempoRuntConnector.cs      ← IRuntConnector (secundario)
        │   └── MockRuntConnector.cs         ← IRuntConnector (DEV/TEST)
        ├── Simit/
        │   ├── SimitConnector.cs
        │   └── MockSimitConnector.cs
        ├── Rues/
        │   ├── RuesConnector.cs
        │   └── MockRuesConnector.cs
        ├── Identity/
        │   ├── VerifikIdentityConnector.cs
        │   └── MockIdentityConnector.cs
        └── Quipux/
            ├── QuipuxConnector.cs
            └── MockQuipuxConnector.cs
```

### Registro en DI por entorno

```csharp
// Flit.Modules.Integrations/Infrastructure/IntegrationsModuleExtensions.cs
public static IServiceCollection AddIntegrationsModule(
    this IServiceCollection services, IHostEnvironment env, IConfiguration config)
{
    if (env.IsDevelopment() || env.IsEnvironment("Test"))
    {
        services.AddScoped<IRuntConnector, MockRuntConnector>();
        services.AddScoped<ISimittConnector, MockSimittConnector>();
        services.AddScoped<IRuesConnector, MockRuesConnector>();
        services.AddScoped<IIdentityValidationConnector, MockIdentityConnector>();
        services.AddScoped<IQuipuxConnector, MockQuipuxConnector>();
    }
    else
    {
        // ConnectorRouter resuelve Verifik → Intempo con failover automático
        services.AddScoped<IRuntConnector>(sp => 
            new ConnectorRouter<IRuntConnector>(
                implementations: [ sp.GetRequiredService<VerifikRuntConnector>(),
                                    sp.GetRequiredService<IntempoRuntConnector>() ],
                logger: sp.GetRequiredService<IIntegrationLogger>(),
                options: sp.GetRequiredService<ConnectorRouterOptions>()));
        // ... resto de conectores
    }
    return services;
}
```

---

## Consecuencias

### Lo que se gana
- Cambio de proveedor RUNT sin tocar handlers de dominio.
- Mocks intercambiables para desarrollo y tests sin configuración de servicios externos.
- Logs automáticos de payload para debugging y auditoría por tenant.
- Hot-failover en <1s entre Verifik e Intempo.
- Preparado para agregar nuevos proveedores (ej. un tercer proveedor RUNT futuro).

### Lo que se pierde / costo aceptado
- Más interfaces y archivos que llamadas HTTP directas.
- El `ConnectorRouter` añade una capa que debe testearse independientemente.
- Los mocks deben mantenerse actualizados con los contratos reales de las interfaces.

---

## ADRs relacionados

- ADR-0004 — Consolidación stack: un solo servicio .NET (Flit.Modules.Integrations vive en core-api).
- ADR-0007 — YARP Gateway: el gateway no ejecuta consultas externas, solo rutea. Las consultas externas son responsabilidad de los módulos.

---

## Notas operativas

- **backend-agent:** Implementar `ConnectorRouter<T>` con Polly ResiliencePipeline (timeout + retry). Credenciales de Verifik desde `.env.verifik` via `IOptions<VerifikOptions>`. Credenciales NUNCA en `IntegrationLog`.
- **security-agent:** Verificar que `RequestPayload` y `ResponsePayload` en `IntegrationLog` no persistan credenciales ni tokens de sesión. Auditar que datos biométricos (fotos de cédula) en `IdentityValidation` solo referencian MinIO (file_ref), no el binario.
- **infra-agent:** Configurar variables de entorno `VERIFIK_API_KEY`, `INTEMPO_API_KEY`, etc. desde secrets; nunca desde `appsettings.json`.
- **qa-agent:** TCs de failover: simular timeout de Verifik → verificar que la consulta continúa con Intempo. TC de log de payload: verificar que `IntegrationLog` se crea para cada consulta con datos correctos.

---

*Creado por: Architecture Agent — 2026-06-10 | Estado: Propuesto*
*Para promover a Aceptado: PR separada con aprobación del Líder Técnico humano*
