# ADR-0004 — Consolidación del stack a .NET 10 + Python ML (eliminación de Go gateway y Node BFF)

- **Estado:** **Aceptado**
- **Fecha:** 2026-05-27
- **Aceptado por:** Jorman Copete (Líder Técnico FLIT)
- **Fecha de aceptación:** 2026-05-27
- **Autor:** Claude Code (sesión interactiva con Líder Técnico)
- **Decisores:** Líder Técnico FLIT (Jorman Copete)
- **Consultados:** Architecture Agent, Code Review Agent, Security Agent, Infra Agent
- **Informados:** Equipo Frontend, Equipo .NET, Equipo Python ML
- **Tags:** arquitectura, backend, consolidación, polígota, .NET, Python

> **📦 Nota de port (2026-05-27):** Este ADR fue portado desde un repo hermano FLIT que tenía un ADR-0002 (microservicios polígota) y un ADR-0004 (BFF reduction). Aquí ese contexto no existe — el repo destino tenía un stack distinto (backend Node Fastify + backend dotnet FLIT.Traspasos + frontend React+Vite). La decisión aplica igual: consolidar a .NET + Python. Las referencias internas a "ADR-0002 microservicios", "ADR-0004 BFF" y "ADR-0008 AWS" en el cuerpo de este ADR refieren al repo origen y se conservan para trazabilidad histórica. No buscar esos ADRs en este repo.

---

## Contexto

[ADR-0002 (2026-05-20)](ADR-0002-arquitectura-microservicios-2026.md) estableció una arquitectura polígota de **5 servicios cooperantes**: `core-api` (.NET 10), `go-gateway` (Go 1.23), `python-ml` (Python 3.13), `node-bff` (Node 22 + Fastify) y `frontend` (Next.js 16). El propio ADR-0002 §1 ("Nota arquitectónica") admite explícitamente que **"la arquitectura objetivo excede los requisitos de negocio actuales"** y se adoptó deliberadamente por aprendizaje del equipo y preparación a escala futura, aceptando trade-offs de:

- TTM **12-18 meses** (vs 4-6 meses con monolito)
- Costo operativo **3-5x**
- Curva de aprendizaje en **5 stacks simultáneos** para un equipo de **6-10 personas**

Después de 7 días de scaffolding y al confrontar el contexto real del producto (200 req/s pico, <100 usuarios concurrentes, 1 VPS Hetzner/Latitude, equipo 6-10 personas), el Líder Técnico solicita revisar la decisión.

### Hallazgos clave que motivan la revisión

| Servicio polígota | Función | ¿.NET 10 lo hace bien? | ¿Justificación técnica única? |
|---|---|---|---|
| `services/go-gateway/` | Routing, JWT RS256, rate limiting | **YARP** (Microsoft) cubre lo mismo. Mismo orden de magnitud en perf. | ❌ |
| `services/node-bff/` (5 módulos productivos + websockets) | CRUD personas/employees/dependents/positions + RUNT + WebSockets | EF Core 10 + Wolverine + SignalR cubren todo nativamente | ❌ |
| `services/python-ml/` | OCR, ML antifraude, validación facial (Tesseract, DeepFace) | **Sí — único caso** donde Python tiene ecosistema insuperable | ✅ |
| `services/core-api/` | Dominio, integraciones DIAN/RUNT/RUES, AOT | Stack canónico | ✅ |

→ De los 4 servicios polígotas, **solo Python tiene justificación técnica real**. Go y Node duplican capacidades que .NET 10 ya entrega.

### Eventos que precipitan la decisión

1. **Hallazgo sobre PDF (2026-05-27):** Playwright/Puppeteer requieren imagen Docker ~1.2 GB. Esto motiva la conversación sobre QuestPDF (.NET nativo, ~5 MB) → ADR-0005 hermano.
2. **Hallazgo sobre archivos (2026-05-27):** El usuario quiere mover todo a VPS local (MinIO + Postgres) revirtiendo el ADR-0008 (AWS S3+DynamoDB) → ADR-0006 hermano.
3. **Consenso del usuario:** "todo VPS local, alto desempeño, queremos algo robusto pero sin sobreingeniería".

### Restricciones que el ADR nuevo debe respetar

| # | Restricción | Origen |
|---|---|---|
| C1 | Habeas Data Ley 1581: cifrado en reposo, audit log inmutable, right-to-erasure | CLAUDE.md, ADR-0002 §11 |
| C2 | Hosting on-prem VPS, no AWS para compute | ADR-0008 §"Clarificación" |
| C3 | OpenAPI contracts-first para todo endpoint público | ADR-0002 §7 |
| C4 | Auth con OpenIddict 6 + JWT RS256 (sigue vigente) | ADR-0002 §8 |
| C5 | Observabilidad OpenTelemetry obligatoria | ADR-0002 §9 |
| C6 | Sistema de agentes IA `.ai/` canónico + sync-agents.sh a 6 IDEs | CLAUDE.md, ADR-0003 |
| C7 | Reglas FLIT 18 innegociables, especialmente regla 13 (ADRs Aceptados solo por LT humano) | `agent-templates/conventions.md` |
| C8 | No hay datos productivos hoy (confirmado por LT 2026-05-27) — refactor seguro sin migración de datos | Conversación 2026-05-27 |
| C9 | Mantener `services/python-ml/` intacto — única razón técnica que justifica polígota | Decisión arquitectónica de este ADR |

---

## Decisión propuesta

**Consolidar el stack backend a 2 servicios cooperantes**:

| Servicio | Stack | Responsabilidad |
|---|---|---|
| `services/core-api/` | **.NET 10 + C# 14 + AOT + Wolverine + EF Core 10 + SignalR + YARP** | **Todo el dominio**: trámites, RBAC, auth, RUNT (vía Verifik), WebSockets, generación PDF (QuestPDF), gestión archivos (cliente MinIO), API Gateway interno (YARP) |
| `services/python-ml/` | **Python 3.13 + FastAPI + uv** | OCR, ML antifraude, validación facial. Sin cambios respecto ADR-0002 |
| `frontend/` | **Next.js 16 + React 19 + Tailwind 4** | Sin cambios respecto ADR-0002 |

### Lo que se elimina

| Path | Acción | Comentario |
|---|---|---|
| `services/go-gateway/` | `git rm -r` | Reemplazado por proyecto `services/core-api/src/Flit.Gateway/` (YARP). Ver [ADR-0007] |
| `services/node-bff/` | `git rm -r` | Sus 5 módulos productivos (`personas`, `employees`, `employee-dependents`, `employee-positions`, `vehicle-query`) se reimplementan en `services/core-api/src/Flit.Modules.*` directamente, sin coexistencia. `websockets/` se reemplaza por SignalR Hub en `Flit.Modules.Notifications`. `files/` se reemplaza por `Flit.Modules.Files` con MinIO (ver [ADR-0006]) |
| `.ai/agents/go-gateway.{agent.yaml,prompt.md}` | `git rm` | Agente sin servicio que custodiar |
| `.ai/agents/node-bff.{agent.yaml,prompt.md}` | `git rm` | Agente sin servicio que custodiar |
| `.ai/agents/backend-engineer-router.{agent.yaml,prompt.md}` | `git rm` | Router entre 3 backends; con un solo backend pierde sentido |

Tras los `git rm` en `.ai/agents/`, ejecutar `pnpm sync:agents` para propagar las eliminaciones a `.claude/`, `.cursor/`, `.github/`, `.agents/`, `.junie/`, `AGENTS.md`.

### Estructura objetivo de `services/core-api/src/`

```
services/core-api/src/
├── Flit.Api/                              # ASP.NET Core host (existente)
├── Flit.Gateway/                          # NUEVO — YARP reverse proxy (ADR-0007)
├── Flit.Infrastructure/                   # (existente)
├── Flit.SharedKernel/                     # (existente)
├── Flit.SharedKernel.Pdf/                 # NUEVO — QuestPDF + plantillas (ADR-0005)
├── Flit.Modules.Auth/                     # (existente)
├── Flit.Modules.Identity/                 # (existente)
├── Flit.Modules.Notifications/            # (existente) + SignalR Hub para WebSockets
├── Flit.Modules.Procedures/               # (existente)
├── Flit.Modules.Rbac/                     # (existente)
├── Flit.Modules.Receipts/                 # (existente)
├── Flit.Modules.TrafficSecretaries/       # (existente)
├── Flit.Modules.Users/                    # (existente)
├── Flit.Modules.Files/                    # NUEVO — MinIO + Postgres (ADR-0006)
├── Flit.Modules.Runt/                     # NUEVO — Verifik client (reemplaza node-bff)
├── Flit.Modules.Personas/                 # NUEVO — migrado desde node-bff
├── Flit.Modules.Employees/                # NUEVO — migrado desde node-bff
├── Flit.Modules.EmployeeDependents/       # NUEVO — migrado desde node-bff
└── Flit.Modules.EmployeePositions/        # NUEVO — migrado desde node-bff
```

### Estructura objetivo de `services/`

```
services/
├── core-api/        # .NET 10 — único backend
└── python-ml/       # Python 3.13 — OCR/ML
```

### Diagrama de despliegue objetivo

```
                    Internet
                       │ HTTPS
                       ▼
                  ┌─────────┐
                  │ Caddy 2 │  auto-SSL
                  └────┬────┘
                       │
       ┌───────────────┼─────────────────────┐
       ▼               ▼                     ▼
  api.flit.co    files.flit.co       admin.flit.co
       │               │                     │
       ▼               ▼                     ▼
  ┌────────┐      ┌────────┐          ┌────────┐
  │core-api│◄────►│ minio  │          │ minio  │
  │ :8080  │ S3   │ :9000  │          │console │
  │        │ API  │        │          │ :9001  │
  │  ├ YARP│      └────┬───┘          └────────┘
  │  ├ Flit.Api│        │
  │  ├ Modules │        ▼
  │  └ SignalR │   ┌─────────┐
  └────┬───────┘   │  disco  │
       │ HTTP      │   VPS   │
       ▼           └─────────┘
  ┌───────────┐
  │ python-ml │  (OCR/ML, consume RabbitMQ)
  │   :8000   │
  └─────┬─────┘
        │
        ▼
   ┌─────────┐  ┌─────────┐  ┌─────────┐
   │postgres │  │rabbitmq │  │  redis  │
   │ :5432   │  │ :5672   │  │ :6379   │
   └─────────┘  └─────────┘  └─────────┘
```

---

## Alternativas consideradas

*(Regla FLIT: mínimo 2, máximo 3 alternativas)*

### Opción 1 — Consolidación a .NET + Python (RECOMENDADA)

**Descripción:** descrita arriba. 2 servicios backend: core-api (.NET 10) + python-ml.

**Pros:**
- 1 solo runtime backend principal: menos OPS, menos pipelines CI/CD, menos imágenes Docker, 1 sola observabilidad consistente
- AOT en .NET 10 da arranque <100ms y RAM <80 MB — perfecto para VPS pequeño
- Wolverine + EF Core + SignalR + YARP + QuestPDF cubren 100% de los casos sin sumar dependencias externas
- Modular Monolith con Vertical Slice mantiene puertas para extraer microservicios cuando (si) la escala lo justifique
- Python ML conserva su ecosistema sin compromiso
- Equipo aprende profundo en .NET en vez de superficial en 4 stacks
- TTM 4-6 meses vs 12-18 meses

**Cons:**
- Pérdida de oportunidad de aprendizaje en Go y Node (parcialmente mitigado: el equipo ya invirtió en scaffolds, no se pierde el aprendizaje conceptual)
- Si el día de mañana llega una US que requiere altísima concurrencia I/O con baja CPU (caso fuerte para Go), hay que reintroducirlo (riesgo bajo dado el dominio)
- core-api crece en superficie — debe respetarse estrictamente la separación por módulos (Wolverine + NetArchTest ayudan)

**Esfuerzo estimado:** L (4-6 semanas para scaffolds + migración funcional vacía + ADRs + sync agentes)
**Riesgos principales:** R1 (ver tabla más abajo)

---

### Opción 2 — Mantener arquitectura polígota actual (ADR-0002 vigente)

**Descripción:** seguir con los 5 servicios. Construir core-api mientras coexiste con node-bff y go-gateway por 12-18 meses.

**Pros:**
- No invalida ADRs aceptados (ADR-0002, ADR-0004, ADR-0008)
- Mantiene oportunidad de aprendizaje en Go y Node
- Cero retrabajo de scaffolds ya creados

**Cons:**
- Mantiene los trade-offs documentados en ADR-0002 §1 (costo 3-5x, TTM 12-18 meses, 5 stacks)
- Doble mantenimiento durante 12-18 meses para los 5 módulos node-bff
- Operación de gateway en Go requiere expertise que el equipo aún no tiene
- ADR-0002 §1 mismo admite que "excede los requisitos de negocio actuales" — esta alternativa perpetúa el problema

**Esfuerzo estimado:** XL acumulado en el tiempo (vs L concentrado)
**Riesgos principales:** burn-out operativo, deuda de aprendizaje superficial

---

### Opción 3 — Monolito .NET puro (sin Python)

**Descripción:** un solo servicio .NET. OCR/ML con bibliotecas .NET (`Tesseract.Net.SDK`, `ML.NET`, `FaceRecognitionDotNet`).

**Pros:**
- Mínima operación: 1 servicio, 1 binario AOT
- Sin red entre servicios para OCR
- Cero serialización para flujo OCR

**Cons:**
- Ecosistema ML/OCR en .NET es **muy inferior** al de Python. `Tesseract.Net.SDK` es wrapper sobre lib nativa con C# bindings limitados; `ML.NET` no tiene equivalentes maduros a DeepFace/InsightFace para validación facial colombiana
- Forzar OCR en .NET genera deuda técnica grande
- Pierde la ventaja real del polígota (caso donde sí aporta valor)

**Esfuerzo estimado:** L pero con riesgo alto de calidad ML
**Riesgos principales:** falsos positivos/negativos en OCR de cédulas y RUT impacta directamente el negocio (trámites colombianos)

---

## Tradeoff aceptado

Elegimos **Opción 1** sobre las demás porque:

1. **Resuelve el problema raíz documentado en ADR-0002 §1** — la arquitectura sobredimensionada para 200 req/s y 6-10 personas
2. **Conserva la única ventaja técnica real del polígota** (Python ML para OCR/cédulas/RUT), sin cargar el costo de las que no aportan
3. **No bloquea el futuro:** Modular Monolith con Vertical Slice + Wolverine permite extraer un módulo a microservicio sin reescribirlo cuando la escala lo justifique
4. **Cumple la regla FLIT 17** ("cambios pequeños y trazables") porque el resto del backend ya es Vertical Slice — la consolidación es estructural, no funcional
5. **Cero datos productivos en juego** (confirmado por LT) → ventana ideal para corregir antes de tener tráfico real

El **costo aceptado** es perder oportunidad de aprendizaje en Go y Node. Mitigación: el equipo ya tiene scaffolds revisables como referencia conceptual (en git history) y los lenguajes pueden estudiarse en proyectos personales sin cargar la operación productiva.

---

## Comparativa cuantitativa

| Criterio | Op.1 (.NET+Py) | Op.2 (5 servicios) | Op.3 (.NET solo) |
|---|---|---|---|
| Servicios backend a operar | 2 | 4 | 1 |
| Stacks que el equipo debe dominar | 2 | 5 | 1 |
| TTM hasta MVP completo | 4-6 sem | 12-18 meses | 4-6 sem |
| RAM total estimada (base) | ~250 MB | ~600 MB | ~150 MB |
| Pipelines CI/CD | 2 | 5 | 1 |
| Calidad OCR cédulas colombianas | ✅✅ (Python ML) | ✅✅ | ⚠️ (.NET pobre) |
| Cumple Habeas Data Ley 1581 | ✅ | ✅ | ✅ |
| Costo VPS estimado/mes | ~$30 | ~$60-80 | ~$25 |
| Re-trabajo desde scaffolds actuales | M (eliminar 2 servicios) | 0 (no hay) | L (rescribir python-ml en .NET) |
| Cumple ADR-0002 vigente | Supersede parcial | ✅ | Supersede + impacta Python |
| Cumple ADR-0004 vigente | Supersede completo | ✅ | Supersede completo |

---

## Consecuencias positivas

- **Operación simplificada:** 1 sola observabilidad, 1 sola estrategia de despliegue, 1 stack de testing para todo el dominio
- **Imágenes Docker más pequeñas:** core-api AOT ~80 MB vs Node 100+ MB + Go 30 MB
- **Latencia interna eliminada:** no hay red entre dominio y gateway/WS/files (todo in-proceso)
- **Mejor uso del Modular Monolith** que ADR-0002 §3 ya prescribe para core-api
- **Pipeline CI/CD reducido:** 2 workflows en vez de 4 (`core-api.yml`, `python-ml.yml`)
- **Reglas de seguridad uniformes:** un solo set de Semgrep/SonarCloud rules para .NET (más Ruff para Python)
- **Equipo más cohesionado:** sin silos por lenguaje en un team de 6-10

## Consecuencias negativas / riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| R1 | Pérdida de scaffold Go y aprendizaje invertido | El scaffold queda en historia git (commits previos) como referencia. Conocimiento conceptual no se pierde. |
| R2 | `Flit.Api` (host único) crece y se convierte en god-process | Wolverine + NetArchTest enforzan separación entre módulos. ArchTests fallan si Modules.Files referencia Modules.Procedures sin pasar por eventos. |
| R3 | Equipo pierde diversidad técnica si toda la carrera del dev es solo .NET | Aceptable: el frontend (Next.js 16 + React 19 + TypeScript) y Python ML ya dan 2 ecosistemas adicionales. |
| R4 | Cuando llegue una US con requisito real de microservicio independiente, extraer toma trabajo | Mitigado por el patrón Vertical Slice: cada módulo es extraíble con interface pública + eventos RabbitMQ. |
| R5 | El ADR-0002 §1 reconoce el aprendizaje como motivación legítima — eliminar Go/Node anula esa motivación | Reconocido. El aprendizaje se traslada al frontend (Next.js 16 Server Actions/Components es nuevo paradigma) y a profundizar .NET 10 AOT + Wolverine. |
| R6 | Romper ADR-0008 (excepción AWS S3+DynamoDB) sin liberación contractual con AWS | Sin contrato, solo cuenta pay-as-you-go. ADR-0006 hermano define migración a MinIO+Postgres. Costo de cierre: $0. |
| R7 | Documentación y agentes IA que referencian go-gateway/node-bff quedan rotos | Actualización: CLAUDE.md, .ai/GUIA.md, MIGRATION_PLAN.md, README.md. Eliminar agentes IA correspondientes y correr `pnpm sync:agents`. Tarea operacional, no técnica. |

---

## Plan de aplicación (PR posterior a aprobación)

> **Este ADR NO implementa cambios.** La implementación es un PR separado (refactor estructural) que solo puede arrancar cuando ADR-0004, ADR-0005, ADR-0006 y ADR-0007 estén en estado **Aceptado** por el Líder Técnico.

### Fase 1 (PR estructural, ~2 días)
1. `git rm -r services/go-gateway/`
2. `git rm -r services/node-bff/`
3. `git rm .ai/agents/go-gateway.* .ai/agents/node-bff.* .ai/agents/backend-engineer-router.*`
4. Limpiar referencias en `.ai/commands/`, `.ai/skills/`, `.ai/GUIA.md`
5. Actualizar `CLAUDE.md`, `docs/MIGRATION_PLAN.md`, `README.md`, `pnpm-workspace.yaml`
6. `pnpm sync:agents` → propagación a 6 IDEs

### Fase 2 (PR scaffolds .NET, ~3 días)
1. Crear `services/core-api/src/Flit.Gateway/` (proyecto YARP) — ver ADR-0007
2. Crear `services/core-api/src/Flit.Modules.Files/` con vertical slice mínima — ver ADR-0006
3. Crear `services/core-api/src/Flit.Modules.Runt/` con cliente Verifik
4. Agregar SignalR Hub a `services/core-api/src/Flit.Modules.Notifications/`
5. Crear `services/core-api/src/Flit.SharedKernel.Pdf/` con QuestPDF — ver ADR-0005
6. Scaffold de los 5 módulos migrados: `Personas`, `Employees`, `EmployeeDependents`, `EmployeePositions` (vacíos, solo estructura)
7. Actualizar `Flit.slnx` y `Directory.Packages.props`

### Fase 3 (PR infra, ~1 día)
1. Reescribir `infra/docker-compose.yml` — eliminar bloques `go-gateway` y `node-bff`, dejar `core-api`, `python-ml`, `postgres`, `redis`, `rabbitmq`, `minio`, `caddy`, observability stack
2. Eliminar `infra/docker-compose.aws.yml` y `infra/aws/` (asociados a ADR-0008 superado por ADR-0006)
3. Crear `infra/minio/init-minio.sh` y `infra/caddy/Caddyfile` — ver ADR-0006

### Fase 4 (PRs de implementación, iterativos)
Cada módulo migrado de node-bff es un PR con su propia US en ADO.

---

## ADRs relacionados

- [ADR-0002](ADR-0002-arquitectura-microservicios-2026.md) — Arquitectura microservicios 2026 (este ADR supersede §2 Decisión 1 y §5 en lo relativo a Go y Node)
- [ADR-0004](ADR-0004-bff-reduction-plan.md) — Plan reducción BFF (este ADR supersede completo)
- [ADR-0008](ADR-0008-excepcion-aws-s3-dynamodb-mvp.md) — Excepción AWS S3+DynamoDB (parte storage superada por ADR-0006 hermano; parte Cognito sigue vigente y se evalúa aparte)
- [ADR-0005] — PDF in-process con QuestPDF (hermano)
- [ADR-0006] — Gestión archivos VPS con MinIO + Postgres (hermano, supersede ADR-0008 parcial)
- [ADR-0007] — API Gateway con YARP en .NET (hermano, define `Flit.Gateway`)

## Compliance

- **Habeas Data Ley 1581:** sin impacto. Cifrado en reposo, audit log y right-to-erasure se mantienen en `Flit.Modules.Files` y `audit.data_access_log`. Más restricciones se evalúan en ADR-0006.
- **Soberanía de datos:** mejora respecto al estado vigente, porque elimina dependencia AWS para storage (ADR-0006 hermano).
- **Auditoría de cambios:** todos los commits del refactor con prefijo `ARCH:` y referencia a este ADR.

---

## Notas operativas para otros agentes

> Completar al promover este ADR a Aceptado.

- **Backend Engineer .NET (`core-dotnet`):** asume el dominio completo. Crea módulos Files, Runt, Personas, Employees, EmployeeDependents, EmployeePositions, SharedKernel.Pdf, Gateway.
- **Python ML (`python-ml`):** sin cambios. Sigue consumiendo eventos RabbitMQ y exponiendo FastAPI privado a core-api.
- **Frontend Engineer:** sin cambios estructurales. El endpoint cambia de `bff.flit.co` a `api.flit.co` (que llega a YARP dentro de core-api).
- **QA Agent:** retirar test plans de go-gateway y node-bff. Migrar Vitest a xUnit cuando los módulos se reimplementen en .NET.
- **Security Agent:** Semgrep solo apuntará a `services/core-api/` (.NET) y `services/python-ml/` (Python). Reglas Node/Go pueden retirarse del pipeline.
- **Infra Agent:** ajustar `infra/docker-compose.yml`, eliminar `infra/aws/`, agregar MinIO + Caddy según ADR-0006 y ADR-0007.
- **Architect (meta-agente):** desde la aceptación, cualquier PR que cree un servicio backend nuevo fuera de `services/core-api/` o `services/python-ml/` se marca como REJECTED salvo aprobación expresa del LT.

---

## Solicitud de aprobación humana

- [ ] Líder Técnico FLIT confirma que ningún cliente productivo está corriendo sobre `services/node-bff/` actualmente
- [ ] Líder Técnico FLIT confirma que la cuenta AWS asociada a ADR-0008 puede cerrarse / dejar de provisionarse sin penalidades contractuales
- [ ] Líder Técnico FLIT confirma asignar 4-6 semanas de equipo para Fases 1-2-3 antes de retomar entrega de features
- [ ] Líder Técnico FLIT promueve ADR-0004, ADR-0005, ADR-0006 y ADR-0007 a **Aceptado** en un único PR de promoción

## Trazabilidad

- **Entrada:** ADR-0002 §1 (admite sobre-dimensión), ADR-0004 (gradualidad), ADR-0008 (AWS storage), conversación 2026-05-27 con Líder Técnico.
- **Salida (cuando se acepte):** PR estructural con `git rm` + scaffolds + actualización de docs. ADRs hermanos 0015, 0016, 0017 quedan vinculados.

---

*ADR generado por Claude Code (Opus 4.7) — 2026-05-27 — sesión interactiva con Líder Técnico Jorman Copete.*
*Estado:* **Aceptado** por el Líder Técnico humano el 2026-05-27 (regla FLIT 13 cumplida).
