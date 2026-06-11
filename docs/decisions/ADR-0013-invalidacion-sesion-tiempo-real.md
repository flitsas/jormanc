# ADR-0013: Invalidación de Sesión en Tiempo Real

**Fecha:** 2026-06-10
**Status:** Propuesto
**Deciders:** Líder Técnico FLIT
**Autor:** Architecture Agent (architecture-agent v2.0)
**Actualizado:** 2026-06-10 — decisión ajustada: IMemoryCache en lugar de Redis (aprobado por LT)
**Tags:** arquitectura, backend, seguridad, jwt, sesiones, memorycache, signalr, rbac
**Relaciona:** ADR-0007 (YARP Gateway — validación JWT), ADR-0010 (multi-tenant)
**Feature ADO:** #9567 IDENTIDAD

---

## Contexto

El Feature #9567 exige que ante un **cambio de privilegios** de un usuario (cambio de roles, revocación de permisos) la sesión del usuario afectado se invalide de forma **inmediata** y el frontend reciba un `403` que dispara una redirección. Texto exacto del feature: *"desalojo inmediato de sesión ante cambio de privilegios (403+redirección)"*.

También requiere: **reset forzado de contraseña** (sesión activa invalida), **olvido de contraseña** (sesiones activas del usuario se invalidan), y **onboarding por invitación** con token temporal.

JWT es stateless por naturaleza — el token emitido es válido hasta su expiración sin que el servidor pueda revocarlo. Esto crea una tensión con el requisito de invalidación inmediata.

El Líder Técnico decidió **no introducir Redis** como componente de infraestructura en esta etapa. El stack actual corre en un único nodo .NET (proceso único), lo cual hace viable una solución con `IMemoryCache` de .NET para el caso de uso inicial.

### Restricciones

| # | Restricción |
|---|---|
| C1 | JWT RS256 ya elegido (ADR-0007 — YARP valida al borde) |
| C2 | Out of scope: SSO, MFA |
| C3 | El frontend debe recibir 403 inmediatamente (no al expirar el token) |
| C4 | Invalidación por: cambio de privilegios, reset/olvido de contraseña, revocación por admin |
| C5 | Multi-tenant: la invalidación debe ser correcta por tenant |
| C6 | **Sin Redis** — decisión del LT |
| C7 | El stack corre en **una instancia única** del proceso .NET (VPS único) |

---

## Decisión

Adoptar **JWT de vida corta (≤15 min) + JTI blacklist en `IMemoryCache` (.NET)** como mecanismo de revocación, complementado con **SignalR push de evento `SessionRevoked`** al frontend cuando el usuario está activo en la aplicación.

---

## Alternativas consideradas

### Opción 1: JWT de vida corta + Refresh Token en BD (sin blacklist)

**Descripción:** JWT de 15 min. Refresh token de larga vida (7 días) en tabla `refresh_tokens` PostgreSQL, cookie HTTPOnly. Para "revocar", se invalida el refresh token en BD. El JWT vigente sigue siendo válido hasta su expiración natural.

**Pros:**
- Sin componente externo (ni Redis ni cache en memoria).
- Patrón conocido y bien documentado.
- Persistencia total: el refresh token sobrevive reinicios del proceso.

**Cons:**
- **No hay invalidación verdadera del JWT en vuelo**: el usuario opera hasta 15 min tras la revocación.
- No cumple el requisito de "desalojo inmediato" del Feature #9567.
- Para reset de contraseña, el usuario puede seguir operando durante la ventana del JWT.

**Esfuerzo estimado:** M
**Riesgos principales:** No cumple el requisito funcional central del Feature #9567.

---

### Opción 2 (recomendada): JWT corto (≤15 min) + JTI blacklist en IMemoryCache + SignalR push logout

**Descripción:** JWT de vida corta (≤15 min) con claim `jti` (JWT ID, UUID único). Al revocar una sesión, el `jti` se agrega a un `IMemoryCache` de .NET con TTL igual al `exp` del JWT (máximo 15 min). El middleware de `Flit.Api` consulta la cache en cada request autenticado. SignalR envía simultáneamente un evento `SessionRevoked` al canal del usuario para logout inmediato en el frontend sin esperar el próximo request HTTP.

**Pros:**
- Sin Redis — cumple la restricción C6.
- Invalidación verdadera: el JWT inutilizable en milisegundos desde la revocación.
- Cumple "desalojo inmediato" del Feature #9567.
- `IMemoryCache` TTL igual al `exp` → las entradas se limpian automáticamente; no hay garbage manual.
- Overhead mínimo: consulta a cache en memoria es O(1) en nanosegundos.
- SignalR ya está en el stack (`Flit.Modules.Notifications`) — sin nueva infraestructura.
- Multi-tenant seguro: la blacklist es por `jti` UUID único — no hay colisión entre tenants.

**Cons:**
- **La blacklist es volátil**: si el proceso .NET reinicia, las entradas de cache se pierden. Los JWT revocados emitidos antes del reinicio vuelven a ser temporalmente válidos hasta su expiración natural (máximo 15 min).
- No funciona si se despliegan **múltiples instancias** del proceso (la cache no es compartida entre instancias). Si el stack escala horizontalmente en el futuro, se requiere un ADR de extensión con Redis o sticky sessions.
- No hay persistencia: el historial de revocaciones no queda auditado en BD por sí solo (se puede mitigar registrando revocaciones en `identity.sessions`).

**Esfuerzo estimado:** S
**Riesgos principales:** Ventana de hasta 15 min de validez de tokens revocados si el proceso reinicia. Inaplicable si el stack escala horizontalmente.

---

### Opción 3: JWT ultracorto (1-2 min) + Refresh Token — sin blacklist ni cache

**Descripción:** JWT de 1-2 min de vida. El frontend refresca automáticamente. La revocación espera a que el JWT actual expire en <2 min.

**Pros:**
- Sin Redis, sin cache. Sólo BD para refresh tokens.
- La ventana de exposición es de 1-2 min, no 15 min.

**Cons:**
- Flood de refresh requests: con 100 usuarios concurrentes → 100 refresh/min adicionales sobre BD.
- "Inmediato" en el Feature implica segundos, no 60-120 segundos.
- Experiencia de usuario degradada: refresh frecuente puede causar parpadeos.

**Esfuerzo estimado:** M
**Riesgos principales:** No cumple "inmediato"; carga adicional en BD.

---

## Tradeoff aceptado

Se elige la **Opción 2 (JWT corto + IMemoryCache + SignalR push)** porque:

1. Es la única opción que cumple "desalojo inmediato" sin Redis.
2. `IMemoryCache` es nativo en .NET, sin dependencia externa.
3. El riesgo de pérdida de blacklist al reiniciar está **mitigado por la vida corta del token (≤15 min)**: en el peor caso, un JWT revocado es válido por los minutos restantes de su TTL.
4. El stack corre en **instancia única** (VPS) — el problema de multi-instancia no aplica en esta etapa.
5. SignalR ya existe en el stack (`Flit.Modules.Notifications`) — el push de logout es un event handler adicional, no infraestructura nueva.

**Condición de revisión de este ADR:** Si el stack escala a 2+ instancias, este ADR debe revisarse y reemplazarse por una solución con Redis o sticky sessions. El `ISessionBlacklist` como interfaz permite el reemplazo sin cambios en los handlers.

---

## Implementación canónica

### JWT Claims

```jsonc
{
  "sub": "user_id_uuid",
  "tid": "tenant_id_uuid",          // tenant claim (ADR-0010)
  "roles": ["operator"],
  "perms": ["tramites.create"],
  "jti": "unique_jwt_id_uuid",       // ← clave para blacklist
  "iat": 1718000000,
  "exp": 1718000900                  // ← iat + 15 min
}
```

### Interfaz de blacklist (sustituible sin cambios en handlers)

```csharp
// Flit.Modules.Identity/Domain/Interfaces/ISessionBlacklist.cs
public interface ISessionBlacklist
{
    Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
}
```

### Implementación con IMemoryCache

```csharp
// Flit.Modules.Identity/Infrastructure/InMemorySessionBlacklist.cs
public sealed class InMemorySessionBlacklist : ISessionBlacklist
{
    private readonly IMemoryCache _cache;

    public Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero)
            _cache.Set($"jti:revoked:{jti}", true, ttl);
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue($"jti:revoked:{jti}", out _));
}
```

### Middleware de verificación (Flit.Api)

```csharp
// Verifica blacklist DESPUÉS de que YARP/Gateway ya validó el JWT RS256
public sealed class JwtBlacklistMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        var jti = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jti is not null && await _blacklist.IsRevokedAsync(jti, ctx.RequestAborted))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { error = "session_revoked" });
            return;
        }
        await next(ctx);
    }
}
```

### SignalR push de revocación (notificación inmediata)

```csharp
// Al revocar la sesión, notificar al frontend via SignalR:
await _hubContext.Clients
    .User(userId.ToString())
    .SendAsync("SessionRevoked", new { reason = "privileges_changed" });
// También: revocar JTI en IMemoryCache
await _blacklist.RevokeAsync(jti, expiresAt);
```

### Tabla `identity.sessions` (registro persistente de revocaciones para auditoría)

```sql
-- Fuente de verdad persistente para bulk-revoke y auditoría
-- (complementa la cache en memoria, no la reemplaza)
CREATE TABLE identity.sessions (
  id          uuid DEFAULT gen_ulid() PRIMARY KEY,
  user_id     uuid NOT NULL REFERENCES identity.users(id),
  tenant_id   uuid NOT NULL,
  jti         text NOT NULL,
  expires_at  timestamptz NOT NULL,
  is_revoked  bool NOT NULL DEFAULT false,
  revoked_at  timestamptz,
  revoked_by  uuid,
  created_at  timestamptz NOT NULL DEFAULT now()
);
-- Al emitir token: INSERT sessions(jti, expires_at)
-- Al revocar: UPDATE sessions SET is_revoked=true, revoked_at=now() WHERE jti=$1
--           + RevokeAsync en IMemoryCache (para validación en tiempo real)
```

### Rehydratación de blacklist al reiniciar

```csharp
// Flit.Api/HostedServices/BlacklistRehydrationService.cs
// Al arrancar: carga los JTI revocados aún vigentes desde identity.sessions
// para minimizar la ventana de validez post-reinicio
public class BlacklistRehydrationService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var activeRevocations = await _db.Sessions
            .Where(s => s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        foreach (var session in activeRevocations)
            await _blacklist.RevokeAsync(session.Jti, session.ExpiresAt, ct);
    }
}
```

> La rehydratación al reiniciar elimina el riesgo de ventana post-restart: la blacklist se reconstruye desde BD en milisegundos al arrancar el proceso.

### Frontend response

```typescript
// shared/api/client.ts — interceptor Axios para 403 session_revoked
client.interceptors.response.use(null, (error) => {
  if (error.response?.status === 403
      && error.response.data?.error === 'session_revoked') {
    queryClient.clear();
    router.push('/login?reason=session_revoked');
  }
  return Promise.reject(error);
});

// shared/hooks/useSignalR.ts — push inmediato (usuario activo en app)
hubConnection.on('SessionRevoked', () => {
  queryClient.clear();
  router.push('/login?reason=session_revoked');
});
```

---

## Consecuencias

### Lo que se gana
- Invalidación en milisegundos sin Redis.
- `IMemoryCache` nativo .NET, sin dependencia externa.
- SignalR push para logout inmediato en frontend.
- Rehydratación al reiniciar minimiza la ventana de riesgo.
- La interfaz `ISessionBlacklist` permite migrar a Redis en el futuro sin cambiar handlers.

### Lo que se pierde / costo aceptado
- La blacklist vive en memoria del proceso: si no hay rehydratación o el proceso reinicia en <15 min tras una revocación, el JWT revocado puede ser temporalmente válido.
- **No funciona con múltiples instancias** del proceso: cada instancia tiene su propia cache. Si se escala horizontalmente, este ADR debe revisarse.
- No hay auditoría de revocaciones "en tiempo real" sin la tabla `identity.sessions` (que sí persiste).

### Tabla comparativa final

| Criterio | Op.1 Refresh Token BD | **Op.2 IMemoryCache + SignalR** ✅ | Op.3 JWT 1-2 min |
|---|---|---|---|
| Invalidación inmediata | ❌ (hasta 15 min) | ✅ ms | ❌ (1-2 min) |
| Sin Redis | ✅ | ✅ | ✅ |
| Sobrevive reinicio | ✅ | ✅ (con rehydratación) | ✅ |
| Multi-instancia | ✅ | ❌ (instancia única) | ✅ |
| Carga extra en BD | Baja | Ninguna (cache) | Alta (refresh flood) |
| Cumple Feature #9567 | ❌ | ✅ | ❌ |

---

## ADRs relacionados

- ADR-0007 — YARP Gateway: valida JWT RS256 al borde. La blacklist se verifica en `Flit.Api` (post-gateway).
- ADR-0010 — Multi-tenant: `jti` es UUID único, no hay colisión entre tenants.

---

## Notas operativas

- **backend-agent:** Implementar `ISessionBlacklist` + `InMemorySessionBlacklist`. Registrar `JwtBlacklistMiddleware` en `Flit.Api`. Implementar `BlacklistRehydrationService` (IHostedService). Registrar `IMemoryCache` con `services.AddMemoryCache()`.
- **infra-agent:** Sin cambios de infraestructura. Si en el futuro el stack escala a 2+ instancias, crear ticket para migrar `InMemorySessionBlacklist` a `RedisSessionBlacklist` con el mismo contrato `ISessionBlacklist`.
- **frontend-agent:** Interceptor Axios en `shared/api/client.ts` para 403+`session_revoked`. Hook `useSignalR` para `SessionRevoked` event. Redirect a `/login?reason=session_revoked`.
- **security-agent:** Auditar que el claim `jti` esté presente en todos los tokens emitidos. Verificar que la blacklist se consulta en cada request autenticado. Confirmar que `BlacklistRehydrationService` se ejecuta al arrancar.
- **qa-agent:** TCs: (1) revocar sesión activa → próximo request devuelve 403, (2) SignalR push → redirect a /login en <500ms, (3) reiniciar proceso → JTI revocado persiste en BD → rehydratación reconstruye blacklist.

---

*Creado por: Architecture Agent — 2026-06-10 | Actualizado: 2026-06-10 (IMemoryCache sin Redis — aprobado por LT)*
*Estado: Propuesto. Para promover a Aceptado: PR separada con aprobación del Líder Técnico humano.*
