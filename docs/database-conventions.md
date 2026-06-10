# Database Conventions — Trámites Digitales de Tránsito y Vehículos

> **Stack**: PostgreSQL 17+, .NET 10 (EF Core 10), ReactJS
> **Tenancy**: Multi-tenant compartido (una BD + `tenant_id`)
> **IDs**: UUID v7
> **Idioma**: Híbrido — estructura técnica en inglés, datos/enums en español
> **Versión**: 1.0 — fuente única de verdad para todo el equipo y los agentes

Este documento es **normativo**. Toda migración debe cumplirlo y será validada automáticamente por la skill `db-schema-validator` (invocada por el `database-agent`) antes de mergear. Las desviaciones requieren un ADR aprobado.

## Gobierno y agentes FLIT

| Agente / skill | Responsabilidad sobre este documento |
|---|---|
| `architecture-agent` | Decide el **modelo de datos de negocio** y el ADR cuando una entidad nueva sienta precedente. Define el "qué" y el "porqué". |
| `database-agent` | **Dueño** de este documento. Materializa el modelo en migraciones que cumplen estas reglas, gestiona schemas, RLS, triggers, índices y catálogos. Define el "cómo". |
| `tech-lead-agent` | Valida DoR/DoD de las HUs de datos y vigila deuda técnica de schema (Modo C / D). |
| `backend-agent` | Implementa la capa de repositorio y acceso a datos siguiendo `docs/data-access-conventions.md`. |
| `db-schema-validator` (skill) | Valida automáticamente cada migración contra el §16 antes de mergear. |

Capa de aplicación asociada: ver **`docs/data-access-conventions.md`** (repositorios, EF Core, contexto de tenant, concurrencia, soft delete en código).

---

## 1. Principios rectores

1. **Convenciones sobre configuración**: una sola forma correcta de hacer cada cosa.
2. **Verificable por máquina**: toda regla aquí debe poder fallar un check automatizado.
3. **Aislamiento de tenant es ley**: ninguna fila de negocio sin `tenant_id` aplicado por RLS.
4. **Inmutabilidad por defecto**: borrados son lógicos (`deleted_at`), no físicos.
5. **Trazabilidad total**: quién hizo qué y cuándo, en cada fila.
6. **Catálogos como tablas, no como ENUM**: salvo enumeraciones realmente estables (≤5 valores que nunca cambian).
7. **PII marcada explícitamente**: cumplimiento Habeas Data verificable por agente.

---

## 2. Organización de schemas (bounded contexts)

Cada bounded context tiene su schema PostgreSQL. Las tablas viven en su schema, no en `public`. `public` queda vacío salvo extensiones.

| Schema | Responsabilidad | Ejemplos de tablas |
|---|---|---|
| `identity` | Tenants, usuarios, roles, permisos, sesiones | `tenants`, `users`, `roles`, `user_roles` |
| `vehicles` | Vehículos, propietarios, historial | `vehicles`, `vehicle_owners`, `vehicle_ownership_history` |
| `procedures` | Trámites: licencias, traspasos, matrículas | `procedures`, `procedure_steps`, `licenses` |
| `infractions` | Comparendos, multas, acuerdos de pago, cursos | `infractions`, `payment_agreements`, `traffic_courses` |
| `payments` | Recaudo, transacciones, conciliación | `payments`, `payment_transactions`, `reconciliations` |
| `catalogs` | Datos de referencia compartidos | `divipola_municipalities`, `vehicle_makes`, `infraction_codes` |
| `notifications` | Notificaciones a ciudadanos y funcionarios | `notifications`, `notification_templates` |
| `integrations` | Mappings y trazas a sistemas externos | `runt_sync_log`, `simit_external_refs` |
| `audit` | Bitácora transversal de cambios | `audit_log` |

**Regla**: una tabla solo puede ser referenciada por FK desde su mismo schema o desde otro schema *de mayor jerarquía*. Ejemplo: `procedures` puede referenciar `vehicles`, pero `vehicles` no puede referenciar `procedures` (evita ciclos entre contextos).

---

## 3. Convenciones de nomenclatura

### 3.1 Generales

| Elemento | Regla | Ejemplo correcto | Ejemplo incorrecto |
|---|---|---|---|
| Schemas | `snake_case`, singular, inglés | `vehicles`, `identity` | `Vehiculos`, `Vehicle` |
| Tablas | `snake_case`, **plural**, inglés | `vehicles`, `payment_agreements` | `Vehicle`, `vehiculo` |
| Columnas | `snake_case`, **singular**, inglés | `license_plate`, `created_at` | `LicensePlate`, `placa` |
| PKs | siempre `id` | `id` | `vehicle_id`, `pk_vehicle` |
| FKs | `<entidad_singular>_id` | `vehicle_id`, `owner_id` | `id_vehicle`, `fk_vehicle` |
| Booleanos | `is_*`, `has_*`, `can_*` | `is_active`, `has_license` | `active`, `license_yn` |
| Timestamps | `*_at` (siempre `timestamptz`) | `created_at`, `paid_at` | `creation_date`, `fecha_pago` |
| Fechas (sin hora) | `*_date` (tipo `date`) | `birth_date`, `expiration_date` | `birth`, `expires_at` (si es solo fecha) |
| Contadores | `*_count` | `attempt_count` | `attempts`, `num_attempts` |
| Montos | `*_amount` | `total_amount`, `fine_amount` | `total`, `value` |
| Códigos | `*_code` | `currency_code`, `infraction_code` | `currency`, `code_infraction` |

### 3.2 Constraints e índices

| Tipo | Patrón | Ejemplo |
|---|---|---|
| Primary key | `pk_<table>` | `pk_vehicles` |
| Foreign key | `fk_<table>_<referenced_table>[_<role>]` | `fk_procedures_vehicles`, `fk_procedures_users_assignee` |
| Unique | `uq_<table>_<column1>_<column2>` | `uq_vehicles_license_plate_tenant` |
| Check | `ck_<table>_<rule>` | `ck_vehicles_license_plate_format` |
| Index (regular) | `ix_<table>_<column1>_<column2>` | `ix_procedures_tenant_id_status` |
| Index (parcial) | `ix_<table>_<column>_partial_<criteria>` | `ix_vehicles_license_plate_partial_active` |
| Trigger | `tr_<table>_<event>_<action>` | `tr_vehicles_before_update_set_updated_at` |

### 3.3 ENUM types (PostgreSQL)

Solo para enumeraciones **realmente estables** (≤5 valores, no varían en la vida del sistema). Para todo lo demás, tabla de catálogo. Cuando se use ENUM:

```sql
CREATE TYPE catalogs.procedure_status_enum AS ENUM (
  'pendiente', 'en_revision', 'aprobado', 'rechazado', 'anulado'
);
```

**Valores siempre en español, snake_case, sin tildes ni eñes en el identificador del enum** (`anulacion`, no `anulación` ni `Anulación`).

---

## 4. Tipos de datos PostgreSQL recomendados

| Caso de uso | Tipo recomendado | Razón |
|---|---|---|
| Identificadores | `uuid` (v7) | Ordenable, distribuible, sin colisiones |
| Texto corto / largo | `text` | Sin penalidad de performance vs `varchar(n)`; usa CHECK para límites |
| Texto con límite duro de negocio | `text` + `CHECK (char_length(...) <= N)` | Más flexible que `varchar(N)` |
| Booleano | `boolean` | — |
| Fecha sin hora | `date` | Cumpleaños, vencimientos sin hora |
| Fecha + hora | `timestamptz` (UTC) | **Nunca** `timestamp` sin tz |
| Monto / dinero | `numeric(15,2)` | **Nunca** `float` ni `real` |
| Coordenadas geográficas | `geography(Point, 4326)` (PostGIS) | Para ubicaciones de comparendos, organismos |
| JSON estructurado | `jsonb` | Solo para datos verdaderamente semi-estructurados |
| Códigos cortos fijos | `char(N)` | Ej. `currency_code char(3)` (ISO 4217) |
| Placa vehicular | `text` + `CHECK` formato | Patrón colombiano (ver §10) |
| Documento de identidad | `text` + `document_type_id` FK | NUIP/CC variables; NIT incluye DV |

### 4.1 Reglas duras

- **Prohibido** `float`, `real`, `double precision` para montos. Siempre `numeric`.
- **Prohibido** `timestamp` sin tz. Siempre `timestamptz`.
- **Prohibido** `serial` / `bigserial` para PK. Siempre `uuid` v7.
- **Prohibido** `json` (sin b). Siempre `jsonb`.

---

## 5. Columnas estándar obligatorias

Toda tabla de negocio (no catálogos, no audit log) debe tener:

```sql
id              uuid        PRIMARY KEY DEFAULT uuidv7(),
tenant_id       uuid        NOT NULL REFERENCES identity.tenants(id),
created_at      timestamptz NOT NULL DEFAULT now(),
created_by      uuid        NOT NULL REFERENCES identity.users(id),
updated_at      timestamptz NOT NULL DEFAULT now(),
updated_by      uuid        NOT NULL REFERENCES identity.users(id),
deleted_at      timestamptz NULL,
deleted_by      uuid        NULL REFERENCES identity.users(id),
row_version     integer     NOT NULL DEFAULT 1
```

**Catálogos** (schema `catalogs`) omiten `tenant_id` y `deleted_*` (usan `is_active boolean` en su lugar). Tienen `created_at`, `updated_at` pero `created_by`/`updated_by` son opcionales (pueden venir de migración inicial).

**Audit log** tiene su estructura propia (ver §11).

### 5.1 `row_version` para concurrencia optimista

EF Core la mapea como `[ConcurrencyCheck]`. Se incrementa por trigger:

```sql
CREATE OR REPLACE FUNCTION audit.increment_row_version()
RETURNS TRIGGER AS $$
BEGIN
  NEW.row_version := OLD.row_version + 1;
  NEW.updated_at := now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

Aplicar trigger `BEFORE UPDATE` en cada tabla de negocio.

### 5.2 Soft delete

- **Borrado lógico**: `UPDATE ... SET deleted_at = now(), deleted_by = :user`
- Todas las queries de negocio filtran `WHERE deleted_at IS NULL` (vía EF Core global query filter o vista).
- Tablas que **no** deben soportar soft delete (raras): documentarlo en el ADR de la tabla.

---

## 6. Multi-tenancy: `tenant_id` + Row Level Security

### 6.1 `tenant_id` obligatorio

Toda tabla en schemas de negocio (`vehicles`, `procedures`, `infractions`, `payments`, `notifications`) tiene `tenant_id NOT NULL` referenciando `identity.tenants(id)`.

**Excepciones permitidas**:
- `catalogs.*` (datos compartidos entre tenants)
- `identity.tenants` (la tabla raíz)
- `audit.audit_log` (tiene su propio `tenant_id` denormalizado)

### 6.2 Row Level Security (RLS)

Toda tabla con `tenant_id` debe tener RLS habilitado con política por defecto:

```sql
ALTER TABLE vehicles.vehicles ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON vehicles.vehicles
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

CREATE POLICY tenant_isolation_insert ON vehicles.vehicles
  FOR INSERT
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

La aplicación .NET setea la variable de sesión en cada request:

```csharp
await dbContext.Database.ExecuteSqlInterpolatedAsync(
  $"SELECT set_config('app.current_tenant_id', {tenantId}, true)");
```

> Ver `docs/data-access-conventions.md` §4 para el interceptor de EF Core que aplica esta variable de forma transversal en cada conexión.

### 6.3 Índices con `tenant_id`

Todo índice de tabla con tenant lleva `tenant_id` como **primera columna**:

```sql
CREATE INDEX ix_procedures_tenant_id_status
  ON procedures.procedures (tenant_id, status);
```

Excepción: el PK por `id` que ya es único globalmente.

---

## 7. UUID v7

PostgreSQL 18+ incluye `uuidv7()` nativo. Para 17, usar extensión `pg_uuidv7` o generar en aplicación.

```sql
-- PostgreSQL 17 (extensión)
CREATE EXTENSION IF NOT EXISTS pg_uuidv7;

-- En la columna:
id uuid PRIMARY KEY DEFAULT uuidv7()
```

En .NET 10: `Guid.CreateVersion7()` (nativo). Configurar EF Core para usarlo en `OnModelCreating`.

**Ventaja crítica**: UUID v7 es ordenable temporalmente → no destruye performance de índices B-tree como v4.

---

## 8. Foreign keys

### 8.1 Reglas

- Toda FK declara explícitamente `ON DELETE` y `ON UPDATE`.
- Por defecto: `ON UPDATE CASCADE ON DELETE RESTRICT`.
- `ON DELETE CASCADE` solo en relaciones de composición real (ej: `procedure_steps` → `procedures`).
- `ON DELETE SET NULL` cuando la relación es opcional histórica.
- Toda FK tiene índice cubriendo su columna.

### 8.2 Ejemplo

```sql
ALTER TABLE procedures.procedures
  ADD CONSTRAINT fk_procedures_vehicles
    FOREIGN KEY (vehicle_id)
    REFERENCES vehicles.vehicles(id)
    ON UPDATE CASCADE
    ON DELETE RESTRICT;

CREATE INDEX ix_procedures_vehicle_id
  ON procedures.procedures (tenant_id, vehicle_id);
```

---

## 9. Catálogos vs ENUMs — criterio de decisión

| Usar **ENUM** si... | Usar **tabla catálogo** si... |
|---|---|
| Valores ≤5 | Valores >5 o crecimiento esperado |
| No cambian en la vida del sistema | Pueden agregarse/inactivarse |
| No necesitan atributos adicionales | Tienen descripción, traducción, código externo |
| Solo se usan en código, no en UI | Aparecen en dropdowns, reportes |

**En este sistema, casi todo es tabla catálogo.** El ENUM se reserva para estados internos de máquinas de estado (ver `procedure_status_enum`).

### 9.1 Estructura estándar de tabla catálogo

```sql
CREATE TABLE catalogs.vehicle_makes (
  id              uuid        PRIMARY KEY DEFAULT uuidv7(),
  code            text        NOT NULL,  -- código RUNT u oficial
  name            text        NOT NULL,
  is_active       boolean     NOT NULL DEFAULT true,
  display_order   integer     NOT NULL DEFAULT 0,
  external_refs   jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- {"runt": "001", "simit": "M-001"}
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_vehicle_makes_code UNIQUE (code)
);
```

---

## 10. Catálogos específicos para Colombia tránsito

| Tabla | Fuente | Notas |
|---|---|---|
| `catalogs.divipola_departments` | DANE | 32 departamentos |
| `catalogs.divipola_municipalities` | DANE | ~1.122 municipios, FK a department |
| `catalogs.document_types` | Registraduría | CC, CE, TI, RC, PA, NIT, NUIP |
| `catalogs.vehicle_makes` | RUNT | Marcas homologadas |
| `catalogs.vehicle_lines` | RUNT | FK a make |
| `catalogs.vehicle_classes` | CNT Art. 2 | Automóvil, motocicleta, camión, etc. |
| `catalogs.service_classes` | CNT Art. 2 | Público, particular, oficial, diplomático |
| `catalogs.body_types` | RUNT | Sedán, hatchback, SUV, etc. |
| `catalogs.fuel_types` | RUNT | Gasolina, diésel, gas, eléctrico, híbrido |
| `catalogs.colors` | RUNT | Estandarizados |
| `catalogs.infraction_codes` | CNT (Ley 769/2002) | Código + descripción + valor en SMMLV |
| `catalogs.procedure_types` | CNT + Ministerio | Matrícula, traspaso, licencia, duplicado, etc. |
| `catalogs.banks` | ACH Colombia / PSE | Para conciliación |
| `catalogs.currencies` | ISO 4217 | Por defecto COP, futuro USD |

### 10.1 Formato de placa colombiana — CHECK constraint

```sql
ALTER TABLE vehicles.vehicles
  ADD CONSTRAINT ck_vehicles_license_plate_format
  CHECK (
    license_plate ~ '^[A-Z]{3}[0-9]{3}$'        -- automóvil/camioneta
    OR license_plate ~ '^[A-Z]{3}[0-9]{2}[A-Z]$'  -- motocicleta
    OR license_plate ~ '^[A-Z]{1,2}[0-9]{4,5}$'   -- oficial/diplomático
  );
```

---

## 11. Auditoría (audit log)

Tabla única en schema `audit`:

```sql
CREATE TABLE audit.audit_log (
  id              uuid         PRIMARY KEY DEFAULT uuidv7(),
  tenant_id       uuid         NOT NULL,
  schema_name     text         NOT NULL,
  table_name      text         NOT NULL,
  record_id       uuid         NOT NULL,
  operation       char(1)      NOT NULL CHECK (operation IN ('I','U','D')),
  changed_by      uuid         NOT NULL,
  changed_at      timestamptz  NOT NULL DEFAULT now(),
  old_values      jsonb        NULL,
  new_values      jsonb        NULL,
  request_id      uuid         NULL,
  ip_address      inet         NULL
);

CREATE INDEX ix_audit_log_tenant_id_changed_at
  ON audit.audit_log (tenant_id, changed_at DESC);
CREATE INDEX ix_audit_log_record
  ON audit.audit_log (schema_name, table_name, record_id);
```

Llenado por trigger genérico en cada tabla de negocio. Tablas con PII alta (ciudadanos, licencias) requieren auditoría obligatoria.

---

## 12. PII y Habeas Data (Ley 1581)

Toda columna que contenga PII debe etiquetarse con `COMMENT`:

```sql
COMMENT ON COLUMN identity.citizens.document_number IS
  '@pii:high @retention:10y @purpose:identification';
```

**Niveles de PII**:
- `@pii:high` — documento de identidad, biométricos, dirección residencial
- `@pii:medium` — email, teléfono, fecha nacimiento
- `@pii:low` — nombre público, profesión

El agente validador escanea estos comments para reportes de cumplimiento. Coordina con `security-agent` (capa Habeas Data) cuando una migración introduce PII nueva.

---

## 13. Plantilla de tabla de negocio

```sql
-- File: db/migrations/V0042__create_table_procedures.sql

SET search_path TO procedures;

CREATE TABLE procedures (
  -- Identificación
  id                  uuid         PRIMARY KEY DEFAULT uuidv7(),
  tenant_id           uuid         NOT NULL,

  -- Datos de negocio
  procedure_type_id   uuid         NOT NULL,
  vehicle_id          uuid         NULL,
  citizen_id          uuid         NOT NULL,
  assigned_to_user_id uuid         NULL,
  status              text         NOT NULL DEFAULT 'pendiente',
  reference_number    text         NOT NULL,
  submitted_at        timestamptz  NOT NULL DEFAULT now(),
  completed_at        timestamptz  NULL,
  total_amount        numeric(15,2) NOT NULL DEFAULT 0,
  currency_code       char(3)      NOT NULL DEFAULT 'COP',
  metadata            jsonb        NOT NULL DEFAULT '{}'::jsonb,

  -- Estándar
  created_at          timestamptz  NOT NULL DEFAULT now(),
  created_by          uuid         NOT NULL,
  updated_at          timestamptz  NOT NULL DEFAULT now(),
  updated_by          uuid         NOT NULL,
  deleted_at          timestamptz  NULL,
  deleted_by          uuid         NULL,
  row_version         integer      NOT NULL DEFAULT 1,

  -- Constraints
  CONSTRAINT fk_procedures_tenants
    FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id),
  CONSTRAINT fk_procedures_procedure_types
    FOREIGN KEY (procedure_type_id) REFERENCES catalogs.procedure_types(id),
  CONSTRAINT fk_procedures_vehicles
    FOREIGN KEY (vehicle_id) REFERENCES vehicles.vehicles(id),
  CONSTRAINT fk_procedures_citizens
    FOREIGN KEY (citizen_id) REFERENCES identity.citizens(id),
  CONSTRAINT fk_procedures_users_assignee
    FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users(id),
  CONSTRAINT fk_procedures_users_creator
    FOREIGN KEY (created_by) REFERENCES identity.users(id),
  CONSTRAINT fk_procedures_users_updater
    FOREIGN KEY (updated_by) REFERENCES identity.users(id),
  CONSTRAINT uq_procedures_reference_number_tenant
    UNIQUE (tenant_id, reference_number),
  CONSTRAINT ck_procedures_status
    CHECK (status IN ('pendiente','en_revision','aprobado','rechazado','anulado','completado')),
  CONSTRAINT ck_procedures_total_amount_non_negative
    CHECK (total_amount >= 0)
);

-- Índices
CREATE INDEX ix_procedures_tenant_id_status
  ON procedures (tenant_id, status) WHERE deleted_at IS NULL;
CREATE INDEX ix_procedures_tenant_id_citizen_id
  ON procedures (tenant_id, citizen_id);
CREATE INDEX ix_procedures_vehicle_id
  ON procedures (vehicle_id) WHERE vehicle_id IS NOT NULL;
CREATE INDEX ix_procedures_submitted_at
  ON procedures (submitted_at DESC);

-- RLS
ALTER TABLE procedures ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedures
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::uuid);

-- Trigger row_version
CREATE TRIGGER tr_procedures_before_update_row_version
  BEFORE UPDATE ON procedures
  FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();

-- Trigger audit
CREATE TRIGGER tr_procedures_audit
  AFTER INSERT OR UPDATE OR DELETE ON procedures
  FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- Comments para PII y agente validador
COMMENT ON TABLE procedures IS '@context:procedures @entity:trámite';
COMMENT ON COLUMN procedures.citizen_id IS '@pii:reference';
COMMENT ON COLUMN procedures.metadata IS '@semi-structured: payloads variables por tipo de trámite';
```

---

## 14. Configuración EF Core (.NET 10)

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseSnakeCaseNamingConvention(); // EFCore.NamingConventions

        modelBuilder.Entity<Procedure>(entity =>
        {
            entity.ToTable("procedures", "procedures");
            entity.Property(p => p.Id).HasDefaultValueSql("uuidv7()");
            entity.Property(p => p.RowVersion).IsConcurrencyToken();
            entity.HasQueryFilter(p => p.DeletedAt == null);
        });
    }
}
```

Paquetes recomendados: `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`.

> El detalle completo de la capa de aplicación (interfaces de repositorio en `domain`, implementación EF Core en `infrastructure`, interceptor de tenant, manejo de concurrencia y transacciones) vive en **`docs/data-access-conventions.md`**.

---

## 15. Anti-patterns prohibidos

| Anti-pattern | Por qué está prohibido |
|---|---|
| `varchar(50)` arbitrario | Adivinar límites; usar `text` + CHECK justificado |
| `created_date date` | Pierde precisión; usar `created_at timestamptz` |
| `is_deleted boolean` | Pierde el cuándo y quién; usar `deleted_at` + `deleted_by` |
| Tabla sin `tenant_id` en schema de negocio | Rompe aislamiento multi-tenant |
| FK sin índice | Locks y queries lentas en cascada |
| `float` para dinero | Errores de redondeo |
| Datos en `public` | Rompe organización por contexto |
| ENUM grande (>5 valores) | Cambiar valores requiere ALTER TYPE costoso |
| PK compuesto en tabla de negocio | Complica joins; usar `uuid` + UNIQUE constraint |
| Reusar `id` como FK name | `vehicle_id`, no `id_vehicle` ni `id` |
| Tablas en singular | `vehicle` está mal; es `vehicles` |
| Mezclar idiomas en una misma tabla | `placa_vehicular` mal; `license_plate` bien |

---

## 16. Checklist de aprobación de tabla

Antes de mergear una migración que cree una tabla nueva, la skill `db-schema-validator` verifica:

- [ ] Está en un schema de bounded context (no en `public`)
- [ ] Nombre en `snake_case`, plural, inglés
- [ ] Tiene `id uuid PRIMARY KEY DEFAULT uuidv7()`
- [ ] Si es de negocio: tiene `tenant_id` con FK a `identity.tenants`
- [ ] Tiene `created_at`, `created_by`, `updated_at`, `updated_by`
- [ ] Soporta soft delete (`deleted_at`, `deleted_by`) salvo justificación en ADR
- [ ] Tiene `row_version` para concurrencia optimista
- [ ] Todas las FK siguen patrón `fk_<table>_<referenced>[_<role>]`
- [ ] Todas las FK declaran `ON DELETE` y `ON UPDATE` explícitos
- [ ] Todas las FK tienen índice cubriendo su columna
- [ ] Si es de negocio: tiene RLS habilitado con política de tenant
- [ ] Índices incluyen `tenant_id` como primera columna
- [ ] No reinventa una tabla existente (validación semántica)
- [ ] Columnas con PII tienen `COMMENT` con etiqueta `@pii:*`
- [ ] Triggers de `row_version` y `audit_log` aplicados
- [ ] Migración incluye `UP` y `DOWN` reversibles
- [ ] ADR vinculado en el PR para entidades nuevas de negocio

---

## 17. Versionado y cambios

Este documento se versiona en `/docs/database-conventions.md`. Cambios requieren PR con ADR (creado por `architecture-agent`, aprobado por el Líder Técnico humano). Introducción del agente y gobierno de datos: [ADR-0008](decisions/ADR-0008-database-agent-convenciones-persistencia.md). La skill `db-schema-validator` debe actualizarse en el mismo PR si se agrega una regla nueva.
