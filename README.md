# FLIT Boilerplate — Agentes IA + Full-Stack

Boilerplate del equipo FLIT con 9 agentes IA integrados al pipeline de desarrollo.

## Quickstart (5 minutos)

```bash
# 1. Instalar dependencias
npm install && cd backend && npm install && cd ../frontend && npm install && cd ..

# 2. Configurar entorno
cp backend/.env.example backend/.env

# 3. Levantar base de datos
cd infra && docker compose up -d postgres && cd ..

# 4. Migraciones
cd backend && npm run migration:run && cd ..

# 5. Iniciar
npm run dev
```

- **Backend**: http://localhost:3000  
- **Frontend**: http://localhost:5173  
- **API health**: http://localhost:3000/health

## Agentes IA disponibles

```bash
# Abrir Claude Code en el repo
claude .

# Comando de entrada recomendado:
# Use the orchestrator to list available flows
```

| Comando | Qué hace |
|---------|----------|
| `/flit:full-flow <feature>` | Pipeline completo Feature → DEV |
| `/flit:implement <US>` | Implementar una US end-to-end |
| `/flit:review <PR>` | Code Review + Security |
| `/flit:deploy <env>` | Deploy a DEV/QA/PDN |
| `/flit:validate dor/dod <item>` | Validar Definition of Ready/Done |

Los agentes aceptan historias desde múltiples fuentes: ID ADO, archivo local, URL o texto directo.

## Estructura del proyecto

```
.ai/agents/         ← Agentes (fuente canónica — funciona en cualquier herramienta IA)
.claude/agents/     ← Adapter Claude Code
.cursor/rules/      ← Adapter Cursor
agent-templates/    ← Plantillas FLIT (DoR, DoD, conventions, etc.)
backend/            ← Node.js API (Clean Architecture)
frontend/           ← React app (feature-sliced)
infra/              ← Docker Compose
docs/decisions/     ← ADRs
```

## Stack

- **Backend**: Node.js 22 + TypeScript + Fastify + TypeORM + PostgreSQL 16
- **Frontend**: React 19 + Vite + TypeScript + TailwindCSS + TanStack Query
- **Tests**: Vitest (unit/integration) + Playwright (E2E)
- **CI/CD**: GitHub Actions

## Para el equipo

- Lee `CLAUDE.md` — convenciones y arquitectura del proyecto  
- Lee `INSTRUCCIONES.md` — guía paso a paso para usar los agentes  


## Crear un nuevo módulo

```bash
# Backend
mkdir -p backend/src/modules/<modulo>/{domain,application,infrastructure,interfaces}

# Frontend  
mkdir -p frontend/src/features/<feature>/{api,components,hooks,pages}
```

Luego invoca:
```
Use the backend-agent to implement story #<ID>
Use the frontend-agent to implement story #<ID>
```

## Agentes IA (Cursor)

Fuente operativa: `.cursor/agents/`, `.cursor/skills/` y `.cursor/rules/`. Editar y commitear directamente; no hay script de sincronización en CI.

---

## Apéndice — Reestructuración hacia microservicios (ADR-0002, en migración)

> El Quickstart de arriba sigue funcionando para la versión actual del boilerplate.
> A partir del 2026-05-20 está en curso una reestructuración hacia plataforma de microservicios.

### Lectura obligatoria si trabajas en esta rama

1. **[`docs/decisions/ADR-0002-arquitectura-microservicios-2026.md`](docs/decisions/ADR-0002-arquitectura-microservicios-2026.md)** — ADR maestro: 12 decisiones arquitectónicas, análisis de 25 componentes, DIFFs para agentes, definiciones de agentes nuevos, plan por 9 fases.
2. **[`docs/MIGRATION_PLAN.md`](docs/MIGRATION_PLAN.md)** — Plan de ejecución con decisiones globales aprobadas, checklist por fase, riesgos y PRs previstos.
3. **[`CLAUDE.md`](CLAUDE.md)** apéndice "Arquitectura objetivo" — referencia rápida de los 5 servicios cooperantes.
4. **[`INSTRUCCIONES.md`](INSTRUCCIONES.md)** apéndice "Arquitectura de microservicios" — comandos operativos durante la transición.

### Stack consolidado (post ADR-0014, 2026-05-27)

| Servicio | Stack | Rol |
|---|---|---|
| `services/core-api/` | .NET 10 + AOT + Wolverine + EF Core 10 + YARP + SignalR + QuestPDF | Todo el backend: dominio, gateway, files (MinIO), websockets, RUNT, PDF, integraciones DIAN/RUES |
| `services/python-ml/` | Python 3.13 + FastAPI + Pydantic v2 | OCR, ML antifraude, validación facial |
| `frontend/` | Next.js 16 + Tailwind 4 + shadcn/ui + Zustand | UI + Server Actions (PWA) |

### Quickstart

```bash
git clone <URL-del-repo>
cd flit-boilerplate
corepack enable && corepack prepare pnpm@latest --activate
pnpm install:all
cd infra && docker compose up -d
# DEV: UI http://localhost:4001 | API http://localhost:4002 (Flit.Gateway YARP, ADR-0017)
```

### Estado de migración

Ver checklist por fase en [`docs/MIGRATION_PLAN.md`](docs/MIGRATION_PLAN.md) §3.3.
Cada fase es un PR independiente contra `develop` con commit convencional.

### Inmutables (no tocar sin OK humano explícito)

- `agent-templates/` (corporativo FLIT)
- `.env.user-identity`, `.env.verifik` (en uso)
- `.cursor/agents/`, `.cursor/skills/`, `.cursor/rules/` — editar en el repo según convenciones FLIT
