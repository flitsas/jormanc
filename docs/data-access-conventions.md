# Data Access & Repository Conventions — FLIT

> **Stack**: .NET 10 (C# 14) + EF Core 10 + Npgsql + PostgreSQL 17+
> **Arquitectura**: Clean Architecture estricta (ADR-0001): `domain → application → infrastructure → interfaces`
> **Capa cubierta**: persistencia — interfaces de repositorio, implementación EF Core, contexto de tenant, concurrencia, transacciones
> **Versión**: 1.0 — fuente única de verdad para la capa de acceso a datos

Este documento es **normativo** y complementa a `docs/database-conventions.md` (schema/DDL). Mientras ese documento define **cómo es la base de datos**, este define **cómo el código accede a ella**. Dueño: `database-agent`. Implementador: `backend-agent`. Validador: `db-schema-validator` + `code-review-agent`.

---

## 1. Principios rectores

1. **El dominio no conoce EF Core.** Las interfaces de repositorio viven en `domain`; las implementaciones en `infrastructure`. El dominio nunca importa `Microsoft.EntityFrameworkCore`.
2. **Una sola forma de leer y escribir**: repositorios + Unit of Work. Prohibido `DbContext` inyectado directamente en use cases o controllers.
3. **El aislamiento de tenant es transversal e implícito**, no responsabilidad de cada query (ver §4).
4. **Soft delete y auditoría son invisibles al caller**: se aplican por filtros globales e interceptores (ver §5 y §6).
5. **Toda escritura es transaccional y explícita.** No hay `SaveChanges` ocultos dispersos.
6. **Las entidades de dominio son puras**: sin atributos ORM. El mapeo va en `IEntityTypeConfiguration<T>`.

---

## 2. Estructura por capas

```
services/core-api/src/Modules/<Context>/
├── Domain/
│   ├── Entities/<Entity>.cs                 # entidad pura, sin EF Core
│   ├── ValueObjects/<Vo>.cs
│   ├── Repositories/I<Entity>Repository.cs  # interfaz (puerto)
│   └── Errors/<Entity>NotFoundError.cs
├── Application/
│   └── UseCases/<Action><Entity>/
│       ├── <Action><Entity>Handler.cs       # depende de I<Entity>Repository
│       └── <Action><Entity>Handler.Tests.cs
└── Infrastructure/
    └── Persistence/
        ├── Configurations/<Entity>Configuration.cs  # IEntityTypeConfiguration
        ├── Repositories/<Entity>Repository.cs        # implementa I<Entity>Repository
        └── Migrations/                               # EF Core migrations
```

`infrastructure` referencia `domain` y `application`. **Nunca al revés.** El `code-review-agent` bloquea cualquier `using` de `Infrastructure` o `Microsoft.EntityFrameworkCore` dentro de `Domain`/`Application`.

---

## 3. Interfaces de repositorio (domain)

### 3.1 Reglas

- Una interfaz por agregado raíz (no por tabla). Las entidades hijas se acceden a través de su raíz.
- Métodos expresan **intención de negocio**, no SQL: `GetActiveByLicensePlateAsync`, no `Where(...)`.
- Retornan entidades de dominio o value objects, **nunca** tipos de EF Core (`IQueryable`, `DbSet`, entidades de infraestructura).
- No exponer `IQueryable` fuera de `infrastructure` — fuga de abstracción y de filtros de tenant/soft-delete.
- Todos los métodos son `async` y aceptan `CancellationToken`.

### 3.2 Plantilla

```csharp
namespace Flit.Procedures.Domain.Repositories;

public interface IProcedureRepository
{
    Task<Procedure?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Procedure?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken ct);
    Task<IReadOnlyList<Procedure>> ListByCitizenAsync(Guid citizenId, CancellationToken ct);
    Task AddAsync(Procedure procedure, CancellationToken ct);
    void Update(Procedure procedure);
    void SoftDelete(Procedure procedure);   // no Remove físico
}
```

> No se expone `tenant_id` en la firma: el tenant lo inyecta el contexto de sesión (§4), no el caller.

---

## 4. Multi-tenancy en código (RLS + contexto de sesión)

El aislamiento se aplica en **dos capas defensivas**:

1. **PostgreSQL RLS** (definido en `database-conventions.md` §6) — última línea de defensa.
2. **Filtro global de EF Core por `tenant_id`** — primera línea, para queries legibles y consistentes.

### 4.1 Acceso al tenant actual

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }   // resuelto desde el JWT / header en cada request
    Guid UserId { get; }
}
```

`ITenantContext` se registra `Scoped` y se llena en el middleware de autenticación, **antes** de cualquier acceso a datos.

### 4.2 Interceptor que activa la variable de sesión RLS

Cada conexión debe setear `app.current_tenant_id` para que la política RLS funcione. Se hace con un `DbConnectionInterceptor`, nunca query por query:

```csharp
public sealed class TenantConnectionInterceptor(ITenantContext tenant) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant, false)";
        var p = cmd.CreateParameter();
        p.ParameterName = "tenant";
        p.Value = tenant.TenantId.ToString();
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

### 4.3 Filtro global por tenant + soft delete

```csharp
modelBuilder.Entity<Procedure>()
    .HasQueryFilter(p => p.TenantId == _tenant.TenantId && p.DeletedAt == null);
```

- **Prohibido** desactivar el filtro de tenant con `IgnoreQueryFilters()` salvo en jobs administrativos cross-tenant explícitamente documentados en un ADR.
- **Prohibido** filtrar tenant manualmente en repositorios (`Where(p => p.TenantId == ...)`) — es responsabilidad del filtro global; duplicarlo invita a inconsistencias.

---

## 5. Soft delete

- Las entidades de negocio implementan `ISoftDeletable` (`DeletedAt`, `DeletedBy`).
- `SoftDelete(entity)` marca `DeletedAt = now`, `DeletedBy = currentUser`; **nunca** `context.Remove(...)` físico.
- El filtro global (§4.3) excluye filas borradas automáticamente. Para incluirlas (auditoría/admin): método de repositorio explícito + `IgnoreQueryFilters()`, documentado.
- Borrado físico requiere ADR aprobado y procedimiento de `infra-agent` (no se hace desde código de aplicación).

---

## 6. Auditoría y campos estándar (interceptor)

Los campos `created_at/by`, `updated_at/by` y `row_version` se llenan por un `SaveChangesInterceptor`, no manualmente en cada handler:

```csharp
public sealed class AuditingInterceptor(ITenantContext ctx) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData e, InterceptionResult<int> result, CancellationToken ct)
    {
        foreach (var entry in e.Context!.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.CreatedBy = ctx.UserId;
            }
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedBy = ctx.UserId;
            }
        }
        return base.SavingChangesAsync(e, result, ct);
    }
}
```

> `row_version` también se incrementa por trigger en BD (`audit.increment_row_version`); en código se mapea como token de concurrencia (§8). El doble mecanismo es intencional: BD es la autoridad, EF detecta conflictos.

---

## 7. Mapeo de entidades (IEntityTypeConfiguration)

- Una clase de configuración por entidad en `Infrastructure/Persistence/Configurations/`.
- Nada de Data Annotations en las entidades de dominio.
- Nombres de tabla/schema explícitos coincidiendo con `database-conventions.md`.

```csharp
public sealed class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    public void Configure(EntityTypeBuilder<Procedure> b)
    {
        b.ToTable("procedures", "procedures");
        b.HasKey(p => p.Id).HasName("pk_procedures");
        b.Property(p => p.Id).HasDefaultValueSql("uuidv7()");
        b.Property(p => p.TotalAmount).HasColumnType("numeric(15,2)");
        b.Property(p => p.RowVersion).IsConcurrencyToken();
        b.HasOne<Vehicle>().WithMany()
            .HasForeignKey(p => p.VehicleId)
            .HasConstraintName("fk_procedures_vehicles")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

`modelBuilder.UseSnakeCaseNamingConvention()` + `ApplyConfigurationsFromAssembly(...)` en `OnModelCreating`.

---

## 8. Concurrencia optimista

- Toda entidad de negocio mapea `row_version` como `IsConcurrencyToken()`.
- Los handlers capturan `DbUpdateConcurrencyException` y la traducen a un error de dominio (`ConcurrencyConflictError`) → HTTP `409 Conflict`.
- Prohibido reintentar a ciegas: recargar, re-aplicar la intención del negocio o devolver el conflicto al cliente.

---

## 9. Transacciones y Unit of Work

- Cada use case de escritura define un límite transaccional. Un `SaveChangesAsync` por use case salvo justificación.
- Para operaciones multi-agregado/multi-schema: `IUnitOfWork` con `BeginTransactionAsync`/`CommitAsync`.
- Prohibido `SaveChanges` dentro de bucles (N escrituras → 1 round-trip cuando sea posible).
- El nivel de aislamiento por defecto de PostgreSQL (`READ COMMITTED`) es el estándar; subir a `SERIALIZABLE` solo con justificación documentada.

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IDisposable> BeginTransactionAsync(CancellationToken ct);
}
```

---

## 10. Queries y performance

| Regla | Detalle |
|---|---|
| `AsNoTracking()` en lecturas puras | Todo método de solo lectura que no actualice debe usarlo |
| Evitar N+1 | Usar `Include`/`ThenInclude` o proyecciones; el `code-review-agent` marca patrones N+1 |
| Proyectar a DTO en lecturas grandes | `Select` a un DTO en vez de materializar el agregado completo |
| Paginación obligatoria | Todo listado expone `skip`/`take`; nunca `ToListAsync()` sin límite sobre tablas de negocio |
| Índices alineados | Las queries frecuentes deben tener índice declarado en la migración (coordinar con `database-agent`) |
| Sin SQL por concatenación | Solo `FromSqlInterpolated`/parámetros; concatenar strings = bloqueante de seguridad |
| `tenant_id` primero | No re-filtrar tenant manualmente (lo hace el filtro global) |

---

## 11. Migraciones EF Core

- Las migraciones son la representación versionada del schema descrito en `database-conventions.md`.
- **Nunca** modificar una migración ya aplicada a cualquier ambiente — crear una nueva.
- Toda migración debe tener `Up` y `Down` reversibles.
- RLS, triggers, políticas y `CHECK` que EF no genera nativamente se agregan con `migrationBuilder.Sql(...)` en la misma migración que crea la tabla.
- El nombre sigue el patrón temporal de EF (`<timestamp>_<DescripcionPascalCase>`); la descripción referencia la HU: `20260602_HU9386_CreateProceduresTable`.
- Antes de mergear, la skill `db-schema-validator` valida el SQL resultante contra el §16 de `database-conventions.md`.

---

## 12. Manejo de errores de persistencia

| Excepción EF/Npgsql | Traducción de dominio | HTTP |
|---|---|---|
| `DbUpdateConcurrencyException` | `ConcurrencyConflictError` | 409 |
| Violación de `uq_*` (`PostgresException 23505`) | `DuplicateResourceError` | 409 |
| Violación de FK (`23503`) | `ReferentialIntegrityError` | 422 |
| Violación de `ck_*` (`23514`) | `BusinessRuleViolationError` | 422 |
| RLS deniega fila (0 filas afectadas) | `ResourceNotFoundError` (no revelar cross-tenant) | 404 |

Nunca propagar `PostgresException` ni mensajes de BD crudos al cliente — fuga de estructura interna.

---

## 13. Seguridad de datos (coordinación Habeas Data)

- Columnas marcadas `@pii:*` en BD: no loguear su valor (Pino/Serilog con redacción). Coordinar con `security-agent`.
- DTOs de salida no exponen PII alta salvo que el endpoint lo requiera y esté autorizado.
- No incluir PII en llaves de caché ni en URLs.

---

## 14. Anti-patterns prohibidos (capa de acceso a datos)

| Anti-pattern | Por qué |
|---|---|
| `DbContext` inyectado en controller/handler | Rompe Clean Architecture; usar repositorio |
| Atributos `[Table]`/`[Column]` en entidad de dominio | Acopla dominio a EF; usar `IEntityTypeConfiguration` |
| Devolver `IQueryable` desde un repositorio | Fuga de abstracción y de filtros de tenant/soft-delete |
| Filtrar `tenant_id` manualmente en cada query | Duplica el filtro global; riesgo de olvido |
| `Remove()` físico en entidades de negocio | Debe ser soft delete |
| `SaveChanges` dentro de bucles | N round-trips |
| `ToList()` sin paginación en tabla de negocio | Riesgo de OOM y queries lentas |
| `IgnoreQueryFilters()` sin ADR | Bypass de aislamiento de tenant |
| SQL por concatenación de strings | Inyección SQL (bloqueante de seguridad) |
| Reintento ciego en conflicto de concurrencia | Pierde la intención del negocio |

---

## 15. Checklist de revisión de PR (capa de datos)

El `code-review-agent` y el `database-agent` verifican:

- [ ] `Domain`/`Application` no importan EF Core ni `Infrastructure`
- [ ] Interfaz de repositorio en `domain`, implementación en `infrastructure`
- [ ] Repositorios no exponen `IQueryable`/`DbSet`
- [ ] Filtro global de tenant + soft delete activos para la entidad
- [ ] No hay filtrado manual de `tenant_id` redundante
- [ ] Lecturas puras usan `AsNoTracking()`
- [ ] Listados paginados
- [ ] Sin patrones N+1 evidentes
- [ ] Concurrencia: `row_version` como token + manejo de `DbUpdateConcurrencyException`
- [ ] Mapeo en `IEntityTypeConfiguration`, no Data Annotations
- [ ] Migración con `Up`/`Down`, RLS/triggers vía `migrationBuilder.Sql`
- [ ] Sin SQL por concatenación
- [ ] Errores de BD traducidos a errores de dominio (no se filtra `PostgresException`)
- [ ] PII no logueada; coordinación con `security-agent` si aplica

---

## 16. Versionado y cambios

Este documento se versiona en `/docs/data-access-conventions.md`. Cambios que afecten el contrato schema↔código requieren actualizar también `docs/database-conventions.md` en el mismo PR, con ADR cuando sienten precedente. Marco de decisión: [ADR-0008](decisions/ADR-0008-database-agent-convenciones-persistencia.md).
