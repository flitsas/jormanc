# ADR-0010: Estrategia Multi-Tenant con Row Level Security PostgreSQL

**Fecha:** 2026-06-10
**Status:** Propuesto
**Deciders:** Líder Técnico FLIT
**Autor:** Architecture Agent (architecture-agent v2.0)
**Tags:** arquitectura, backend, postgresql, multi-tenant, rls, seguridad
**Relaciona:** ADR-0008 (convenciones persistencia), ADR-0001 (clean architecture)
**Features ADO:** Transversal a todos los features (#9567 al #9566)

---

## Contexto

FLIT 2.0 es una plataforma SaaS que sirve a múltiples **compañías/organismos de tránsito** (tenants) desde una única instalación. El Feature #9567 exige **aislamiento estricto por tenant**: ningún usuario de una compañía debe poder ver ni modificar datos de otra.

El Feature #9565 (Admin Compañías) introduce el concepto de **Super Administrador** que sí tiene acceso multi-tenant (visibilidad cruzada de datos).

El stack ya define PostgreSQL 17+ con soporte nativo a Row Level Security (`docs/database-conventions.md`). La decisión de arquitectura principal es **cómo implementar el aislamiento físico/lógico entre tenants**.

### Restricciones

| # | Restricción |
|---|---|
| C1 | Stack fijo: PostgreSQL 17+ (no negociable — ADR-0004) |
| C2 | Un solo VPS inicial (no justifica infraestructura de múltiples instancias) |
| C3 | SuperAdmin necesita visibilidad cross-tenant para soporte y administración |
| C4 | El número de tenants estimado: 5-50 compañías en el primer año |
| C5 | Habeas Data (Ley 1581): datos de personas por tenant son sensibles — aislamiento es requisito legal además de técnico |
| C6 | EF Core 10 es el ORM (code-first, migraciones automáticas) |

---

## Decisión

Adoptar **Shared Database + Shared Schema + `tenant_id` en todas las tablas de negocio + Row Level Security (RLS) PostgreSQL**, con contexto de tenant inyectado via `SET LOCAL app.tenant_id` en cada transacción.

---

## Alternativas consideradas

### Opción 1: Schema PostgreSQL por tenant

**Descripción:** Un schema PostgreSQL (ej. `tenant_acme`, `tenant_bogota_ot`) por cada compañía. Las mismas tablas en cada schema. El contexto de tenant se resuelve por `search_path`.

**Pros:**
- Aislamiento físico fuerte: un error de aplicación no puede filtrar datos entre schemas.
- Migraciones por tenant independientes (rollback de un tenant sin afectar a otros).
- Backup/restore selectivo por tenant trivial.
- Fácil auditoría: un `pg_dump` por schema = datos del tenant.

**Cons:**
- Migraciones deben aplicarse a N schemas en cada deploy → complejidad operacional proporcional a N tenants.
- EF Core no soporta natively multi-schema en un solo DbContext para el mismo modelo → requiere infraestructura custom.
- Queries cross-tenant (SuperAdmin) requieren UNION con N schemas o vistas cross-schema.
- Con 50 tenants → 50 schemas × 30 tablas = 1.500 tablas en la BD → pg_catalog crece.
- Creación de tenant nuevo requiere ejecutar migración del schema completo → lento.

**Esfuerzo estimado:** L
**Riesgos principales:** Complejidad operacional no lineal; EF Core migrations custom; tiempo de onboarding de nuevo tenant ~30-60s.

---

### Opción 2: Base de datos PostgreSQL por tenant

**Descripción:** Una instancia PostgreSQL (o base de datos separada) por tenant. Connection string diferente por tenant.

**Pros:**
- Máximo aislamiento: un tenant no puede en ninguna circunstancia acceder a datos de otro.
- Backup/restore individual trivial.
- Migraciones independientes por tenant.
- Cumplimiento normativo de aislamiento más fácil de demostrar.

**Cons:**
- Inviable en un VPS único con 50 tenants: 50 instancias PostgreSQL × RAM mínima = inviable.
- Connection pooling por tenant complejo (un pool por BD).
- EF Core requiere DbContext factory por tenant con connection string dinámica.
- Cada nuevo tenant requiere provisionar BD, usuario PostgreSQL y ejecutar migraciones → proceso lento y propenso a errores.
- SuperAdmin requiere consultar N bases de datos separadas para reportes cross-tenant.
- Actualizaciones de schema sincronizadas entre N BDs en deploys son el mayor riesgo operacional.

**Esfuerzo estimado:** L+
**Riesgos principales:** Inviable en infraestructura VPS única; no escala en la fase actual.

---

### Opción 3 (recomendada): Shared Database + tenant_id + RLS PostgreSQL

**Descripción:** Una única base de datos PostgreSQL compartida. Todas las tablas de negocio tienen `tenant_id uuid NOT NULL`. PostgreSQL Row Level Security (RLS) garantiza que cada conexión solo ve filas de su tenant. El contexto de tenant se establece via `SET LOCAL app.tenant_id = '<uuid>'` al inicio de cada transacción.

**Pros:**
- Una sola BD con una sola instancia PostgreSQL → operación simple en VPS.
- Onboarding de nuevo tenant: INSERT en `identity.tenants` → inmediato.
- Migraciones aplicadas una vez → afectan todos los tenants simultáneamente.
- EF Core estándar con un solo DbContext y un solo connection string.
- RLS enforceado a nivel de motor de BD → un bug de aplicación no puede filtrar datos entre tenants.
- SuperAdmin bypass simple: no SET el tenant_id o usar política especial.
- Connection pooling eficiente con PgBouncer (un pool compartido).
- Alineado al 100% con `docs/database-conventions.md` (ya define este patrón).

**Cons:**
- Un error en la política RLS puede exponer datos cross-tenant → requiere tests de aislamiento en QA.
- Backup/restore de un solo tenant requiere `pg_dump --where "tenant_id = ..."` → menos trivial.
- Migraciones afectan todos los tenants simultáneamente (feature flags para rollout gradual si se necesita).
- `SET LOCAL app.tenant_id` debe ejecutarse en CADA transacción → requiere middleware confiable.
- Con crecimiento extremo (>500 tenants con millones de filas), puede requerir particionamiento por `tenant_id`.

**Esfuerzo estimado:** M
**Riesgos principales:** Configuración incorrecta de RLS; middleware que olvide SET tenant_id; SuperAdmin bypass mal implementado.

---

## Tradeoff aceptado

Se elige la **Opción 3 (Shared DB + RLS)** porque:

1. La infraestructura actual (VPS único) hace inviables Opción 1 y 2 a escala.
2. PostgreSQL RLS es exactamente la feature diseñada para este caso de uso.
3. El patrón ya está documentado en `docs/database-conventions.md` — no se está inventando nada.
4. El costo de un bug de RLS se mitiga con tests automatizados de aislamiento en el pipeline (qa-agent).
5. El equipo puede evolucionar a particionamiento por tenant_id si el volumen lo requiere sin cambiar el patrón de aplicación.

---

## Implementación canónica

### 1. Columna tenant_id en toda tabla de negocio

```sql
-- Cada tabla de negocio incluye:
tenant_id uuid NOT NULL REFERENCES identity.tenants(id) ON DELETE RESTRICT,

-- Índice compuesto obligatorio:
CREATE INDEX ix_<tabla>_tenant_id ON <schema>.<tabla>(tenant_id, id)
  WHERE deleted_at IS NULL;
```

### 2. Política RLS canónica

```sql
-- Habilitar RLS en tabla
ALTER TABLE procedures.procedures ENABLE ROW LEVEL SECURITY;
ALTER TABLE procedures.procedures FORCE ROW LEVEL SECURITY;

-- Política de aislamiento (aplica a todos los roles salvo bypass explícito)
CREATE POLICY tenant_isolation ON procedures.procedures
  AS PERMISSIVE FOR ALL
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);
```

### 3. Middleware TenantContext en .NET

```csharp
// Flit.Infrastructure/Middleware/TenantContextMiddleware.cs
public sealed class TenantContextMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        var tenantId = ctx.User.FindFirstValue("tid")
            ?? throw new UnauthorizedAccessException("Missing tenant claim");

        await using var scope = ctx.RequestServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlitDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            $"SET LOCAL app.tenant_id = '{tenantId}'");

        await next(ctx);
    }
}
```

### 4. SuperAdmin bypass

```csharp
// Si el usuario tiene rol superadmin, no se SET el tenant_id
// → RLS ve current_setting('app.tenant_id', true) como NULL
// → La política PERMISSIVE no bloquea (NULL::uuid es falsy en la condición)

// Política alternativa explícita para SuperAdmin:
CREATE POLICY superadmin_bypass ON procedures.procedures
  AS PERMISSIVE FOR ALL
  TO flit_superadmin_role     -- ← rol PostgreSQL especial para el usuario de conexión del SA
  USING (true);
```

### 5. RLS en EF Core (transacciones explícitas)

```csharp
// Flit.Infrastructure/Persistence/FlitDbContext.cs
protected override void OnModelCreating(ModelBuilder mb)
{
    // El tenant_id filter se aplica via RLS en BD, NO en EF Core (HasQueryFilter)
    // Razón: HasQueryFilter es bypasseable; RLS no.
    base.OnModelCreating(mb);
}
```

**Regla:** No usar `HasQueryFilter` para tenant isolation. Confiar en RLS. El `HasQueryFilter` se puede usar para soft delete (`deleted_at IS NULL`) que no es de seguridad.

---

## JWT Claims y propagación

El JWT incluye el claim `tid` (tenant_id). El `Flit.Gateway` valida el JWT y propaga `X-Tenant-Id` header. El `TenantContextMiddleware` en `Flit.Api` consume el header (no re-valida el JWT — eso ya lo hizo el Gateway).

```
Browser → Flit.Gateway (valida JWT RS256) → X-Tenant-Id header → Flit.Api → TenantContextMiddleware → SET LOCAL app.tenant_id
```

---

## Consecuencias

### Lo que se gana
- Operación simple con un solo PostgreSQL y un solo DbContext.
- Aislamiento enforceado a nivel de motor de BD (no confiamos solo en la aplicación).
- Onboarding de nuevo tenant en milisegundos.
- Compatible con PgBouncer transaction-mode pooling.

### Lo que se pierde / costo aceptado
- Tests de aislamiento de tenant son obligatorios en QA (qa-agent debe generar TCs específicos).
- Backup selectivo por tenant requiere script custom.
- Si el volumen supera los 500 tenants activos con >10M filas por tabla, se necesita particionamiento (revisar en ese momento con un ADR de extensión).

---

## ADRs relacionados

- ADR-0008 — Convenciones normativas de persistencia (dueño: database-agent)
- ADR-0009 — Motor de parametrización (usa tenant_id en procedure_types, form_fields, rule_sets, etc.)

---

## Notas operativas

- **database-agent:** Aplicar `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` + política `tenant_isolation` en TODAS las tablas de negocio de todos los schemas. Crear rol PostgreSQL `flit_superadmin_role` con política bypass.
- **backend-agent:** Implementar `TenantContextMiddleware`. Verificar que toda transacción ejecute `SET LOCAL` antes de cualquier query. No usar `HasQueryFilter` para tenant isolation.
- **security-agent:** Auditar que ningún endpoint omita el middleware; incluir prueba de aislamiento cross-tenant en checklist de seguridad.
- **qa-agent:** Generar TCs de aislamiento: usuario de Tenant A NO puede ver datos de Tenant B en ningún endpoint. TC de SuperAdmin viendo datos cross-tenant. TC de revocación de sesión respeta tenant.

---

*Creado por: Architecture Agent — 2026-06-10 | Estado: Propuesto*
*Para promover a Aceptado: PR separada con aprobación del Líder Técnico humano*
