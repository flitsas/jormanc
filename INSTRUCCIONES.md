# INSTRUCCIONES.md — Cómo usar el Boilerplate FLIT con Agentes IA

Guía paso a paso para el equipo. Sin saltar pasos.

---

## Parte 1 — Configuración inicial (solo una vez)

### Paso 1: Clonar e instalar

```bash
git clone <URL-del-repo>
cd flit-boilerplate
npm install          # instala workspace raíz
cd backend && npm install
cd ../frontend && npm install
```

### Paso 2: Variables de entorno

```bash
cp backend/.env.example backend/.env
# Edita backend/.env con tus valores de base de datos
```

### Paso 3: Levantar la base de datos local

```bash
# Requiere Docker Desktop corriendo
cd infra
docker compose up -d postgres
```

Verifica que PostgreSQL está listo:
```bash
docker compose ps   # postgres debe estar en "healthy"
```

### Paso 4: Correr migraciones

```bash
cd backend
npm run migration:run
```

### Paso 5: Iniciar el proyecto completo

```bash
# En la raíz
npm run dev
# Backend en http://localhost:3000
# Frontend en http://localhost:5173
```

Prueba que funciona:
```bash
curl http://localhost:3000/health
# → {"status":"ok","env":"development"}
```

---

## Parte 2 — Configurar los agentes IA

### Para Claude Code (recomendado)

Los agentes en `.claude/agents/` se cargan automáticamente cuando abres Claude Code en este directorio.

```bash
# Abrir Claude Code en el repo
claude .
```

Comandos disponibles (escríbelos en el chat):
```
/flit:full-flow          # Pipeline completo Feature → DEV
/flit:implement          # Implementar una US
/flit:review             # Review de una PR
/flit:deploy             # Deploy a ambiente
/flit:decompose          # Descomponer Feature en US
/flit:health             # Health check del repo
/flit:validate           # Validar DoR/DoD
/flit:crear-feature      # Crear una nueva Feature
/flit:crear-hu           # Crear una nueva User Story (HU)
/flit:gestion-hu         # Gestionar/refinar una User Story
/flit:gestion-qa         # Gestionar tareas de QA para una User Story
/flit:consultar-vehiculo # Consulta RUNT/Verifik por VIN o Placa + Documento
```

**Ejemplos de uso de `/flit:consultar-vehiculo`:**
```
/flit:consultar-vehiculo                                            # modo conversacional (pregunta paso a paso)
/flit:consultar-vehiculo vin=LRW3E7FS8TC752304                      # consulta por VIN
/flit:consultar-vehiculo placa=VEJ918 tipoDoc=NIT numDoc=811011779  # consulta por placa
```
Requiere `.env.verifik` configurado (copia `.env.verifik.example` y completa `VERIFIK_API_TOKEN`). El resultado se muestra como tabla HTML inline y se guarda en `docs/reports/vehicle-queries/`.

**Primera prueba — el orquestador:**
```
Use the orchestrator to list available flows
```

### Para Cursor

1. Abre el repo en Cursor
2. Las reglas en `.cursor/rules/flit-agents.mdc` se aplican automáticamente
3. Para activar un agente específico, pega el contenido de `.ai/agents/<nombre>.md` como instrucción en el chat

### Para GitHub Copilot

Las instrucciones en `.github/copilot-instructions.md` se aplican automáticamente en VS Code con Copilot.

Para activar agentes específicos: pega el contenido de `.ai/agents/<nombre>.md` como comentario de contexto.

### Para OpenAI Codex / Antigravity / otras herramientas

1. Lee `CLAUDE.md` (en la raíz) — es la fuente de verdad
2. Copia el contenido del agente que necesitas desde `.ai/agents/<nombre>.md`
3. Úsalo como system prompt o instrucción de contexto en tu herramienta

### Mantener agentes en Cursor

Edita y commitea directamente `.cursor/agents/`, `.cursor/skills/` y las reglas en `.cursor/rules/`.

---

## Parte 3 — Flujo de trabajo diario

### 1. Empezar con una Feature nueva

**Opción A — Con el orquestador (recomendado):**
```
Use the orchestrator to run FULL_FEATURE for: <pega aquí la descripción de la Feature o da el ID>
```
El orquestador te preguntará cómo tienes la información y manejará todo el flujo.

**Opción B — Usando el comando específico de creación:**
```
/flit:crear-feature
```
Este comando te guiará paso a paso para documentar una nueva Feature siguiendo el estándar del proyecto. También puedes usar un agente explícito: `Use the tech-lead-agent (mode A) to draft a feature about <necesidad>`.

### 2. Descomponer la Feature en User Stories

```
Use the orchestrator to run DECOMPOSE_FEATURE for: <referencia>
```
O directamente:
```
Use the tech-lead-agent (mode B) to decompose feature #4520
```

El agente acepta:
- ID de ADO: `#4520`
- Archivo local: `docs/features/F-REGISTRO.md`
- URL: `https://dev.azure.com/.../workitems/4520`
- Texto directo: pega el contenido

También puedes usar `/flit:crear-hu` si deseas redactar una User Story individual desde cero, el cual te asistirá con el formato y criterios necesarios.

### 3. Gestionar o Refinar una User Story

Para refinar, revisar o actualizar una User Story existente (validar estructura, completar criterios):
```
/flit:gestion-hu <referencia>
```
Este comando analizará la US y te ayudará a completarla o mejorarla para que cumpla con el *Definition of Ready*.

### 4. Gestionar QA

Para definir casos de prueba o verificar la calidad de una US:
```
/flit:gestion-qa <referencia>
```
Este comando te asistirá en la creación de planes de prueba y la validación exhaustiva de los criterios de aceptación.

### 5. Implementar una User Story

```
Use the orchestrator to run IMPLEMENT_US for: story #4521
```

Esto automatiza: implementación → code review → security → merge.

### 6. Revisar una Pull Request

```
Use the orchestrator to run REVIEW_PIPELINE for PR: !456
```

### 7. Deploy

```
Use the orchestrator to run DEPLOY_ENV to: qa build #890
```

### 8. Validar DoR antes de Active

```
Use the tech-lead-agent (mode C) to validate DoR for story #4521
```

O con comando slash:
```
/flit:validate dor #4521
```

---

## Parte 4 — Crear un nuevo módulo (ejemplo: Contratos)

### Estructura backend

```bash
# Crea los directorios
mkdir -p backend/src/modules/contratos/{domain,application,infrastructure,interfaces}
mkdir -p backend/tests/unit/contratos backend/tests/integration/contratos
```

Archivos a crear (en orden):
1. `domain/contrato.entity.ts` — entidad pura
2. `domain/contrato.repository.interface.ts` — interface del repositorio
3. `application/contratos.use-cases.ts` — use cases
4. `infrastructure/contrato.typeorm-entity.ts` — entidad TypeORM
5. `infrastructure/contrato.typeorm-repository.ts` — implementación
6. `interfaces/contratos.dto.ts` — schemas Zod
7. `interfaces/contratos.controller.ts` — controller Fastify
8. `interfaces/contratos.routes.ts` — routes
9. `migrations/<timestamp>-CreateContratosTable.ts`
10. Registrar routes en `src/app.ts`

Con el agente:
```
Use the architecture-agent to design the solution for the Contratos module
Use the backend-agent to implement story #4525  (la US [BACKEND] de Contratos)
```

### Estructura frontend

```bash
mkdir -p frontend/src/features/contratos/{api,components,hooks,pages}
```

Con el agente:
```
Use the frontend-agent to implement story #4526  (la US [FRONTEND] de Contratos)
```

---

## Parte 5 — Cómo agregar un agente nuevo

1. Crea el archivo en `.cursor/agents/<nombre>-agent.md` siguiendo los existentes
2. Sigue la estructura de los agentes actuales (Rol, Reglas, Pre-flight, etc.)
3. Incluye el Story Intake Protocol si el agente procesa work items
4. Commitea el agente y, si aplica, una regla en `.cursor/rules/`

---

## Referencia rápida

| Tarea | Comando |
|-------|---------|
| Ver flujos disponibles | `Use the orchestrator to list available flows` |
| Pipeline completo | `/flit:full-flow <feature>` |
| Crear Feature | `/flit:crear-feature` |
| Crear US | `/flit:crear-hu` |
| Descomponer Feature | `/flit:decompose <feature>` |
| Gestionar US | `/flit:gestion-hu <US>` |
| Gestionar QA | `/flit:gestion-qa <US>` |
| Implementar US | `/flit:implement <US>` |
| Review PR | `/flit:review <PR>` |
| Deploy | `/flit:deploy <env> <build>` |
| Validar DoR | `/flit:validate dor <item>` |
| Validar DoD | `/flit:validate dod <item>` |
| Health check | `/flit:health` |
| Consultar vehículo (VIN) | `/flit:consultar-vehiculo vin=<VIN>` |
| Consultar vehículo (Placa) | `/flit:consultar-vehiculo placa=<PLACA> tipoDoc=<NIT\|CC\|CE\|PA\|TI> numDoc=<NUMERO>` |
| Consultar vehículo (interactivo) | `/flit:consultar-vehiculo` |
| Agentes Cursor | `.cursor/agents/`, `.cursor/skills/` |

## Solución de problemas frecuentes

**El agente pide ADO CLI y no lo tengo:**
> El agente detecta que el CLI no está disponible y te ofrecerá recibir la historia en texto directo. Responde con la opción 4 (texto directo) y pega el contenido de la historia.

**El agente no aparece en Claude Code:**
> Verifica que el archivo tiene frontmatter YAML correcto en `.claude/agents/`. Ejecuta `(editar `.cursor/agents/` y `.cursor/skills/` directamente)`.

**Error de base de datos al iniciar:**
> Verifica que Docker está corriendo: `docker compose ps` en `infra/`. Verifica las variables en `backend/.env`.
