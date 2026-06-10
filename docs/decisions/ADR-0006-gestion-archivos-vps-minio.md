# ADR-0006 — Gestión de archivos en VPS con MinIO + PostgreSQL

- **Estado:** **Aceptado**
- **Fecha:** 2026-05-27
- **Aceptado por:** Jorman Copete (Líder Técnico FLIT)
- **Fecha de aceptación:** 2026-05-27
- **Autor:** Claude Code (sesión interactiva con Líder Técnico)
- **Decisores:** Líder Técnico FLIT (Jorman Copete)
- **Consultados:** Architecture Agent, Backend .NET Agent, Security Agent, Infra Agent
- **Informados:** Equipo Frontend, Equipo .NET, equipo Compliance/Habeas Data
- **Tags:** arquitectura, infra, storage, minio, postgres, vps, habeas-data

> **📦 Nota de port (2026-05-27):** ADR portado desde repo hermano FLIT donde supersedeaba un ADR-0008 (excepción AWS S3+DynamoDB). En este repo ese ADR no existe — la decisión MinIO+Postgres aplica directamente sin nada que reemplazar. Referencias a "ADR-0008 AWS S3+DynamoDB" en el cuerpo refieren al repo origen y se conservan para trazabilidad histórica.

---

## Contexto

[ADR-0008 (Aceptado 2026-05-21)](ADR-0008-excepcion-aws-s3-dynamodb-mvp.md) estableció el uso de **AWS S3 + AWS DynamoDB** para almacenamiento de archivos y metadata durante el MVP, como excepción consciente al ADR-0002 §"Infraestructura compartida" (que recomienda MinIO + Postgres). Las razones documentadas eran:

- AWS ya provisionado (cuenta + bucket + tabla)
- Aprendizaje del equipo en AWS SDK
- DynamoDB single-table design como ejercicio NoSQL
- Costo aceptado: $30-100/mes para el MVP

El propio ADR-0008 §"Decisión propuesta" enumera **triggers de re-evaluación**: costo >$200/mes sostenido, soberanía regulatoria, fallas AWS >4h/trimestre, escala >10TB.

### Evento que precipita la re-evaluación

El **2026-05-27**, el Líder Técnico FLIT confirma:

1. **Cambio de visión arquitectónica**: "todo VPS local, alto desempeño, sin sobreingeniería"
2. **Cero datos productivos** en AWS S3 o DynamoDB hasta hoy (solo scaffolding)
3. **Sin contrato AWS comprometido**: cuenta pay-as-you-go, costo de cierre $0
4. **Soberanía de datos**: trámites colombianos con cédulas/RUT/fotos en EE.UU. (us-east-1) generan obligación Art. 26 Ley 1581 que se prefiere evitar
5. **Consolidación de stack** ([ADR-0004 hermano](ADR-0004-consolidacion-stack-dotnet-python.md)) elimina `services/node-bff/` donde vivía el módulo `files/` con AWS SDK v3

Ninguno de los triggers numéricos del ADR-0008 disparó. **El trigger real es estratégico y arquitectónico**, formalizado en este ADR.

### Restricciones

| # | Restricción | Origen |
|---|---|---|
| C1 | Subida/descarga directa cliente ↔ storage (sin pasar por API) | ADR-0008 §"Diseño MVP" (preservar el patrón presigned URL) |
| C2 | URLs prefirmadas con expiry corto: 5min subida, 10min descarga | ADR-0008 §C3, ajustado |
| C3 | Buckets sin acceso público | ADR-0008 §C4 |
| C4 | Cifrado en reposo SSE-S3 (AES-256) | Habeas Data + Ley 1581 |
| C5 | Audit log inmutable de cada acceso (presigned URL generada) | ADR-0002 §11 |
| C6 | Right-to-erasure por anonimización (no hard-delete del registro) | ADR-0002 §11, Ley 1581 |
| C7 | Lifecycle: archivos temporales expiran a 7-30 días | ADR-0008 §"S3 lifecycle" |
| C8 | Backup off-site obligatorio | nuevo (mitiga riesgo VPS single-node) |
| C9 | Soberanía: datos en Colombia o en su defecto LATAM | Sustitución estricta del Art. 26 Ley 1581 |
| C10 | Hospedaje del módulo Files **dentro de `services/core-api/`** | ADR-0004 (consolidación) |

---

## Decisión propuesta

**Adoptar MinIO (object storage) + PostgreSQL (metadata) como pareja única de almacenamiento de archivos para FLIT.** Eliminar dependencia AWS S3 y AWS DynamoDB. El módulo `Flit.Modules.Files` en `services/core-api/` orquesta ambos via el patrón presigned URL.

### Componentes

| Componente | Stack | Rol |
|---|---|---|
| **MinIO** | Contenedor Docker (binario Go ~100 MB) | Object storage S3-compatible. Vive en `infra/docker-compose.yml` como servicio aparte |
| **PostgreSQL schema `files`** | Mismo cluster Postgres de core-api | Tabla `files.documents` con metadata + JSONB (reemplazo de DynamoDB) |
| **`services/core-api/src/Flit.Modules.Files/`** | .NET 10 + AWS SDK for .NET (S3-compatible con MinIO) | Vertical slice: request upload URL, confirm upload, request download URL, delete |
| **Caddy 2** | Contenedor Docker | Reverse proxy con auto-SSL: `files.flit.co` → `minio:9000`, `admin.flit.co` → `minio:9001` (con IP allowlist) |

### Topología

```
                    Internet (browser)
                          │ HTTPS :443
                          ▼
                  ┌───────────────┐
                  │   Caddy 2     │  auto-SSL Let's Encrypt
                  └───────┬───────┘
                          │ red Docker "flit-net"
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
   api.flit.co     files.flit.co     admin.flit.co
        │                 │                 │
        ▼                 ▼                 ▼
  ┌──────────┐      ┌──────────┐    ┌──────────┐
  │ core-api │─────►│  minio   │    │  minio   │
  │  :8080   │ S3   │  :9000   │    │  console │
  │          │ API  │          │    │  :9001   │
  │  Modules.│      └────┬─────┘    └──────────┘
  │   Files  │           │ bind-mount o volumen
  └────┬─────┘           ▼
       │            ┌─────────────┐
       │ EF Core    │   disco     │
       │ schema     │ /mnt/storage│
       │  files     │   /minio    │
       ▼            └─────────────┘
  ┌──────────┐
  │ postgres │
  │  :5432   │
  └──────────┘
```

### Flujo de subida (presigned URL)

```
1. Frontend                                    POST /api/files/upload-url
   { scope: "tramite", scopeId: "uuid",        ─────────────────────────►
     filename: "cedula.pdf", contentType:
     "application/pdf", sizeBytes: 2048000 }

2. core-api (Flit.Modules.Files)
   ├─ valida RBAC (¿puede subir a este scope?)
   ├─ valida mime/size (mime allowlist + max 25 MB)
   ├─ INSERT files.documents (status='pending', expires_at=NOW()+24h)
   ├─ AmazonS3.GetPreSignedURLAsync(PUT, 10min, ContentType, ContentLength)
   └─ retorna { fileId, uploadUrl, expiresAt }

3. Frontend  PUT https://files.flit.co/{bucket}/{key}?X-Amz-...
             body: binario PDF  ────────────► MinIO valida firma → 200

4. Frontend  POST /api/files/{fileId}/confirm

5. core-api  ├─ HEAD object en MinIO (verifica existencia y tamaño)
             ├─ Lee ETag → asigna como sha256 inicial
             ├─ UPDATE files.documents SET status='ready', confirmed_at=NOW()
             ├─ INSERT audit.data_access_log (action='FILE_UPLOADED', actor, scope)
             └─ Publica evento "FileUploaded" → RabbitMQ
                                              ├─ python-ml consumer (OCR si cédula)
                                              └─ Notifications consumer (SignalR push)
```

### Flujo de descarga

```
1. Frontend  GET /api/files/{fileId}/download-url
2. core-api  ├─ valida RBAC (¿puede descargar este archivo en este scope?)
             ├─ AmazonS3.GetPreSignedURLAsync(GET, 5min)
             ├─ INSERT audit.data_access_log (action='FILE_DOWNLOAD_URL_ISSUED')
             └─ retorna { downloadUrl, expiresAt }
3. Frontend  GET https://files.flit.co/{bucket}/{key}?X-Amz-... → MinIO sirve binario
```

### Schema PostgreSQL (`files`)

```sql
CREATE SCHEMA IF NOT EXISTS files;

CREATE TABLE files.documents (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    scope            VARCHAR(50)  NOT NULL,            -- 'tramite' | 'recibo' | 'identidad' | ...
    scope_id         UUID         NOT NULL,            -- FK lógico al aggregate dueño
    bucket           VARCHAR(100) NOT NULL,
    object_key       VARCHAR(500) NOT NULL UNIQUE,
    filename         VARCHAR(255) NOT NULL,
    content_type     VARCHAR(100) NOT NULL,
    size_bytes       BIGINT,
    sha256           VARCHAR(64),                       -- llenado al confirmar
    status           VARCHAR(20)  NOT NULL DEFAULT 'pending',  -- pending|ready|deleted|failed
    metadata         JSONB        NOT NULL DEFAULT '{}',-- equivalente a campos schemaless DynamoDB
    created_by       UUID         NOT NULL,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    confirmed_at     TIMESTAMPTZ,
    expires_at       TIMESTAMPTZ,                       -- solo en status=pending; cron limpia
    deleted_at       TIMESTAMPTZ                        -- anonimización en lugar de hard-delete
);

CREATE INDEX idx_files_scope        ON files.documents (scope, scope_id);
CREATE INDEX idx_files_pending      ON files.documents (status) WHERE status = 'pending';
CREATE INDEX idx_files_expires      ON files.documents (expires_at) WHERE status = 'pending';
CREATE INDEX idx_files_metadata_gin ON files.documents USING GIN (metadata);
```

JSONB cubre los campos schemaless que DynamoDB tenía en ADR-0008 (`tipo`, `source`, `ownerUserId`, etc.). Las queries por atributo arbitrario se resuelven con índice GIN.

### Configuración MinIO (resumen)

```yaml
# infra/docker-compose.yml — fragmento (detalle completo en PR de implementación)
minio:
  image: minio/minio:RELEASE.2026-01-15T00-00-00Z
  command: server /data --console-address ":9001"
  environment:
    MINIO_ROOT_USER:     ${MINIO_ROOT_USER}
    MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD}
    MINIO_SERVER_URL:    https://files.flit.co       # CRÍTICO: firma con hostname público
    MINIO_BROWSER_REDIRECT_URL: https://admin.flit.co
    MINIO_PROMETHEUS_AUTH_TYPE: public
  volumes:
    - minio-data:/data                                # en prod: bind-mount a disco dedicado
  networks: [flit-net]
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
```

### Buckets iniciales

| Bucket | Lifecycle | Cifrado | Comentario |
|---|---|---|---|
| `tramites` | none | SSE-S3 | Documentos asociados a trámites (cédulas, soportes) |
| `identidad` | none | SSE-S3 | Documentos PII estables (RUT firmado, certificados oficiales) |
| `recibos` | expire 5 años (compliance contable) | SSE-S3 | PDFs generados por QuestPDF (ADR-0005) |
| `temporales` | expire 7 días | SSE-S3 | Uploads en progreso, drafts, exports |

### Service accounts MinIO

| Account | Permisos | Uso |
|---|---|---|
| `flit-root` | admin total | Solo para administración manual desde Console |
| `flit-core` | `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject`, `s3:HeadObject`, `s3:ListBucket` solo sobre los 4 buckets | core-api en `Flit.Modules.Files` |
| `flit-backup` | `s3:GetObject`, `s3:ListBucket` (read-only) | Job de mirror a Backblaze B2 |

### Backup off-site (DR)

Job `minio-backup` corre `mc mirror` cada 6h hacia **Backblaze B2** (o Cloudflare R2 como alternativa):

```bash
mc mirror --overwrite --remove local/identidad b2/flit-dr-identidad
mc mirror --overwrite --remove local/tramites  b2/flit-dr-tramites
mc mirror --overwrite --remove local/recibos   b2/flit-dr-recibos
# 'temporales' NO se respalda (datos efímeros)
```

Costo estimado: <$1/mes en MVP, ~$6/TB/mes a escala.

### Hospedaje del módulo `Flit.Modules.Files`

```
services/core-api/src/Flit.Modules.Files/
├── Flit.Modules.Files.csproj
├── Domain/
│   ├── FileDocument.cs                 # entidad agregada
│   ├── FileStatus.cs                   # enum
│   └── IFileStorage.cs                 # puerto (genera presigned URLs, HEAD, DELETE)
├── Application/
│   ├── RequestUploadUrl/
│   │   ├── RequestUploadUrlCommand.cs
│   │   ├── RequestUploadUrlHandler.cs
│   │   └── RequestUploadUrlResponse.cs
│   ├── ConfirmUpload/
│   ├── RequestDownloadUrl/
│   └── DeleteFile/
├── Infrastructure/
│   ├── MinIOFileStorage.cs             # implementación IFileStorage con AWS SDK
│   ├── FilesDbContext.cs               # EF Core DbContext (schema files)
│   └── Migrations/                     # EF Core migrations
└── Interfaces/
    ├── FilesEndpoints.cs               # ASP.NET Core minimal API
    └── FilesDtos.cs                    # con FluentValidation
```

### AWS SDK for .NET vs Minio SDK oficial

**Decisión:** usar **`AWSSDK.S3`** (no `Minio` SDK) con `ForcePathStyle = true`.

Razones:
- Mantiene la opción de migrar a S3 real sin reescribir código si algún día se decide
- Ecosistema más amplio (logging, metrics, OTel integraciones)
- El equipo aprende un SDK que aplica a más empleadores futuros

Trade-off: paquete ~3 MB vs ~300 KB de Minio SDK. Aceptable.

---

## Alternativas consideradas

### Opción 1 — MinIO + Postgres con módulo Files en core-api (RECOMENDADA)

Descrita arriba.

**Pros:**
- Soberanía total de datos (Colombia / VPS propio)
- Sin facturas AWS recurrentes
- Misma transacción ACID que el dominio (metadata files vive en mismo Postgres)
- Mismo SDK que usaba node-bff (AWS S3) → cero cambio mental
- JSONB en Postgres cubre el caso schemaless que DynamoDB tenía
- Backup off-site barato (Backblaze B2 ~$6/TB/mes)
- Cumple ADR-0004 (consolidación en core-api)

**Cons:**
- MinIO single-node es SPOF (mitigado con backup + plan claro de migración a MinIO distribuido erasure-coded cuando aplique)
- Operación de Caddy + MinIO + backup script suma 3 piezas de infra (mitigado: todo es Docker, IaC con compose)
- Sin lifecycle nativo tan rico como AWS (mitigado: MinIO sí soporta lifecycle con `mc ilm`, suficiente para los casos descritos)

**Esfuerzo:** M (4-5 días: docker-compose + scripts init + módulo Files + tests + caddyfile)
**Riesgos principales:** R1, R2 (ver tabla más abajo)

---

### Opción 2 — Mantener AWS S3 + DynamoDB (ADR-0008 vigente)

**Descripción:** seguir con AWS, como ADR-0008 prescribe.

**Pros:**
- No invalida ADR-0008
- Lifecycle nativo, versioning nativo, CloudTrail nativo
- Sin riesgo SPOF a nivel storage

**Cons:**
- $30-100/mes recurrente
- Habeas Data Art. 26: transferencia internacional de PII (cédulas, RUT) → obliga cláusula en política de privacidad y aviso al usuario
- Latencia VPS Colombia ↔ AWS us-east-1: 80-150ms por HEAD/GET (problema para muchas operaciones de listado)
- Contradice nueva visión arquitectónica del LT (todo VPS local)
- Mantiene dependencia AWS SDK + IAM rotation 90d
- Requiere `services/node-bff/` para hospedar el módulo files (contradice ADR-0004)

**Esfuerzo:** 0 (ya implementado)
**Riesgos:** factura creciente, soberanía, latencia

---

### Opción 3 — PostgreSQL Large Objects o BYTEA (sin object storage)

**Descripción:** guardar bytes del archivo directamente en Postgres (columna `BYTEA` o vía `lo_*` API).

**Pros:**
- Cero infraestructura adicional
- ACID extrema con el resto del dominio
- Backup unificado (pg_dump cubre todo)

**Cons:**
- **No escala**: BYTEA con archivos > 100KB destroza Postgres (TOAST + WAL bloat). VACUUM se vuelve insostenible
- Sin URLs directas: cada upload/download pasa por el API .NET → bandwidth y RAM penalizados
- pg_dump engorda en GBs rápidamente
- Sin separación de concerns: tu BD relacional se convierte en file store

Rechazada de plano. Mismo veredicto que ADR-0008 §"Alternativa C".

---

## Tradeoff aceptado

Elegimos **Opción 1 (MinIO + Postgres)** sobre las demás porque:

1. **Cumple la visión expresa del LT** (todo VPS local) sin trade-offs de soberanía
2. **Elimina dependencia AWS** sin perder el patrón clave (presigned URL → upload/download directo)
3. **Encaja con ADR-0004** (consolidación a core-api) — el módulo Files vive en .NET
4. **Cero costo de cierre** desde AWS (sin contrato, sin datos productivos)
5. **Habeas Data se simplifica** (datos en Colombia, sin Art. 26)
6. **MinIO es S3-compatible** → el código del módulo Files es portable de vuelta a AWS si algún día se decide (ej. trigger técnico R1)

**Costo aceptado:**
- SPOF en MinIO single-node hasta que se migre a cluster (mitigado por backup off-site)
- Operación de 3 piezas adicionales en VPS (Caddy + MinIO + backup script) — pero todo IaC en docker-compose, no manual

---

## Comparativa

| Criterio | Op.1 MinIO+PG | Op.2 AWS S3+DynamoDB | Op.3 BYTEA |
|---|---|---|---|
| Soberanía de datos (Colombia) | ✅ | ❌ (us-east-1) | ✅ |
| Costo MVP (~1k usuarios) | $0 (VPS pagado) | $30-100/mes | $0 |
| Costo a 10TB | $20/mes infra | $230/mes | N/A no escala |
| Latencia API ↔ storage | <5ms (red Docker) | 80-150ms (Internet) | 0 (in-process pero penaliza Postgres) |
| Cifrado en reposo | ✅ SSE-S3 | ✅ SSE-S3 | ✅ pgcrypto |
| Habeas Data Art. 26 | No aplica | **Aplica** (transferencia internacional) | No aplica |
| Lifecycle | ✅ `mc ilm` | ✅ nativo | ⚠️ custom |
| Versioning | ✅ MinIO opcional | ✅ nativo | ❌ |
| Backup off-site | Backblaze ~$1/mes MVP | CloudTrail + replicación AWS | pg_dump (incluye bytes, pesado) |
| Audit log | `audit.data_access_log` (custom) | CloudTrail + custom | `audit.data_access_log` |
| Right-to-erasure | DELETE objeto + UPDATE files.documents | DELETE objeto + UPDATE DynamoDB | DELETE row |
| Cumple ADR-0004 consolidación | ✅ (módulo en core-api) | ❌ (requiere node-bff) | ✅ |
| Vendor lock-in | Cero | Moderado | Cero |
| SPOF storage | ⚠️ single-node MinIO | ✅ S3 multi-AZ | ✅ Postgres replicado |

---

## Consecuencias positivas

- Soberanía completa de datos personales (cédulas, RUT, fotos)
- Cierre de cuenta AWS posible sin penalidad (costo MVP→0)
- Latencia mucho menor para operaciones HEAD/LIST (red Docker)
- Misma transacción ACID para metadata + dominio (Postgres único)
- Módulo Files dentro de `services/core-api/` consolidado con el resto del dominio
- Patrón presigned URL preservado (no impacta UX ni frontend)
- Backup off-site barato y geográficamente distribuido (Backblaze US/EU)

## Consecuencias negativas / riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| F1 | MinIO single-node es SPOF | Backup off-site cada 6h con Backblaze B2. Plan de migración a MinIO distribuido (erasure coding, 4+ nodos) cuando RTO/RPO lo requieran |
| F2 | Disco VPS lleno bloquea uploads | Monitor Prometheus alerta a 70%/85%/95%. Bind-mount a volumen Cloud (Hetzner Volume) ampliable en caliente |
| F3 | Operación de Caddy + MinIO + backup script suma piezas IaC | Todo en `infra/docker-compose.yml` versionado en git. Runbook en `docs/runbooks/MINIO_OPERATIONS.md` |
| F4 | Habeas Data: si MinIO se compromete, todos los archivos quedan accesibles | Cifrado SSE-S3 + service accounts con permisos mínimos + audit log + IP allowlist en `admin.flit.co` |
| F5 | Latencia inicial cliente ↔ MinIO depende de red VPS (no CDN) | Aceptable: usuarios colombianos en VPS colombiano. Si llega expansión LATAM, agregar Cloudflare en frente con cache TTL bajo |
| F6 | Eliminar ADR-0008 puede confundir al equipo si no se comunica claramente | ADR-0008 NO se borra: queda en `Superseded` con link a ADR-0006. Actualizar `MIGRATION_PLAN.md` y `MVP_TRAMITE_VEHICULAR.md` §3 |
| F7 | Tarea de eliminar bucket S3 y tabla DynamoDB en AWS queda pendiente | Checklist post-migración: revocar IAM keys, vaciar bucket, eliminar tabla. Documentado en plan de aplicación |
| F8 | El runbook `MVP_TRAMITE_VEHICULAR.md` §3 describe flujo con node-bff y AWS | Se actualiza el runbook como parte del PR de implementación de este ADR |

---

## Plan de aplicación (PR posterior a aprobación)

> **Este ADR NO implementa cambios.** Implementación es PR separado tras aceptación.

### Fase A — Infra (1 día)
1. Agregar bloque `minio`, `minio-init`, `caddy`, `minio-backup` a `infra/docker-compose.yml`
2. Crear `infra/minio/init-minio.sh` (idempotente: buckets + lifecycle + service accounts)
3. Crear `infra/caddy/Caddyfile` con `files.flit.co`, `admin.flit.co`, `api.flit.co`
4. Generar secrets MinIO con `openssl rand -base64 32`, cifrar con sops, almacenar en `infra/secrets/`
5. Smoke test local: `docker compose up -d minio minio-init caddy`, verificar `curl https://files.flit.co/minio/health/live`

### Fase B — Módulo Flit.Modules.Files (2-3 días)
1. Crear proyecto `services/core-api/src/Flit.Modules.Files/Flit.Modules.Files.csproj`
2. Agregar `AWSSDK.S3` a `Directory.Packages.props`
3. Implementar Domain (FileDocument, FileStatus, IFileStorage)
4. Implementar Infrastructure (MinIOFileStorage, FilesDbContext, EF Core migration con schema `files`)
5. Implementar Application (4 vertical slices: RequestUploadUrl, ConfirmUpload, RequestDownloadUrl, DeleteFile)
6. Implementar Interfaces (FilesEndpoints + FilesDtos + FluentValidation)
7. Registrar en DI desde `Flit.Api/Program.cs`
8. Tests: `Flit.Modules.Files.Tests/` con MinIO Testcontainer

### Fase C — Cleanup AWS (0.5 días, manual)
1. Líder Técnico: revocar IAM access keys del bucket `tramites-files-*`
2. Vaciar buckets S3 (`aws s3 rm --recursive s3://tramites-files-*`)
3. Eliminar tabla DynamoDB `tramites-files`
4. Eliminar archivos `infra/aws/`, `infra/docker-compose.aws.yml`, `infra/localstack/`
5. Eliminar variables AWS de `.env.example`
6. Cerrar cuenta AWS si no quedan otros recursos (Cognito sigue activo por ADR-0008 parcial)

### Fase D — Documentación (0.5 días)
1. Actualizar `docs/runbooks/MVP_TRAMITE_VEHICULAR.md` §3 (flujo S3+DynamoDB → MinIO+Postgres)
2. Marcar ADR-0008 como `Superseded` (parcial, solo S3+DynamoDB)
3. Crear `docs/runbooks/MINIO_OPERATIONS.md` con comandos `mc`, recuperación, restore desde Backblaze
4. Actualizar `CLAUDE.md` con referencia a ADR-0006 y nueva arquitectura
5. Actualizar `MIGRATION_PLAN.md` removiendo fases que dependían de AWS/node-bff/files

**Total estimado:** 4-5 días con 1 ingeniero.

---

## ADRs relacionados

- [ADR-0002 §6 "Datos y mensajería"](ADR-0002-arquitectura-microservicios-2026.md) — establece MinIO como infraestructura recomendada (este ADR ejecuta esa recomendación)
- [ADR-0008](ADR-0008-excepcion-aws-s3-dynamodb-mvp.md) — **superseded por este ADR en la parte S3+DynamoDB**. La parte AWS Cognito sigue vigente y se evalúa en ADR futuro separado
- [ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md) — hermano (consolidación stack)
- [ADR-0005](ADR-0005-pdf-in-process-questpdf.md) — hermano (los PDFs generados se almacenan vía este módulo Files)
- [ADR-0007](ADR-0007-api-gateway-yarp.md) — hermano (Caddy rutea a YARP del core-api)

## Compliance

- **Habeas Data Ley 1581:**
  - Cifrado en reposo SSE-S3 (AES-256) en MinIO
  - Audit log inmutable en `audit.data_access_log` por cada presigned URL emitida
  - Right-to-erasure: DELETE objeto MinIO + UPDATE `files.documents SET status='deleted', deleted_at=NOW()` (la fila queda como traza)
  - Datos en VPS Colombia → **no aplica Art. 26** (transferencia internacional eliminada)
- **Ley 594 de 2000:** PDFs en bucket `recibos` con lifecycle 5 años cumplen retención contable
- **OWASP A01 (Broken Access Control):** RBAC en cada generación de presigned URL + scope/scopeId validados contra dominio
- **OWASP A02 (Cryptographic Failures):** TLS 1.3 en Caddy, SSE-S3 en MinIO, secretos cifrados con sops

---

## Notas operativas para otros agentes

- **Backend Engineer .NET (`core-dotnet`):** propietario de `Flit.Modules.Files`. Implementa 4 vertical slices + EF migration + tests con Testcontainers.
- **Frontend Engineer:** ajustar cliente API para los 4 endpoints (`POST /api/files/upload-url`, `POST /api/files/{id}/confirm`, `GET /api/files/{id}/download-url`, `DELETE /api/files/{id}`). El patrón es idéntico al actual con AWS; solo cambia el dominio del presigned URL (`files.flit.co` en vez de `*.s3.amazonaws.com`).
- **QA Agent:** Test Cases nuevos para upload/download presigned URL con MinIO real (Testcontainer en CI), incluir RBAC y validación mime/size.
- **Security Agent:**
  - Validar que `MinIO__AccessKey/SecretKey` se carguen desde sops, no de `.env` plano
  - Verificar política mínima del service account `flit-core`
  - Auditar Caddyfile para que `admin.flit.co` tenga IP allowlist + basic auth
- **Infra Agent:** dueño de `infra/docker-compose.yml`, `infra/minio/`, `infra/caddy/`. Verificar bind-mount a disco dedicado en prod.

---

## Solicitud de aprobación humana

- [ ] Líder Técnico FLIT confirma cierre/desprovisión de bucket S3 y tabla DynamoDB AWS (cero datos productivos perdidos)
- [ ] Líder Técnico FLIT confirma proveedor de DR off-site (Backblaze B2 sugerido, alternativa Cloudflare R2)
- [ ] Líder Técnico FLIT confirma dominios `files.flit.co` y `admin.flit.co` disponibles para apuntar al VPS
- [ ] Líder Técnico FLIT confirma decisión sobre AWS Cognito (alcance fuera de este ADR — evaluar en ADR futuro)
- [ ] Líder Técnico FLIT promueve ADR-0006 a **Aceptado** junto con ADRs 0014, 0015, 0017 en PR de promoción

## Trazabilidad

- **Entrada:** ADR-0008 (excepción AWS S3+DynamoDB Aceptado 2026-05-21), conversación 2026-05-27 con Líder Técnico sobre VPS local
- **Salida (cuando se acepte):** PR `infra-files` con docker-compose actualizado + módulo `Flit.Modules.Files` scaffolded + ADR-0008 marcado `Superseded` parcial

---

*ADR generado por Claude Code (Opus 4.7) — 2026-05-27.*
*Estado:* **Aceptado** por el Líder Técnico humano el 2026-05-27 (regla FLIT 13 cumplida).
