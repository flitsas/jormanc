# AGENTS.override.md — frontend

> Override por servicio para Codex/Junie. Editar directamente en este archivo.
> Override por servicio. **Codex** y **Junie** leen este archivo + `AGENTS.md` raíz cuando se invocan desde `frontend/`.

## Agente principal: `frontend-engineer`

Agente principal para este servicio.

## Scope

`frontend/**`

## Lectura obligatoria al trabajar en este servicio

- [`AGENTS.md` raíz](../../AGENTS.md) — convenciones globales FLIT
- [`.ai/agents/frontend-engineer.prompt.md`](../../.ai/agents/frontend-engineer.prompt.md) — prompt completo del agente
- [`.ai/agents/frontend-engineer.agent.yaml`](../../.ai/agents/frontend-engineer.agent.yaml) — metadata, quality_gates, triggers
- [`agent-templates/conventions.md`](../../agent-templates/conventions.md) — 18 reglas innegociables
- [`agent-templates/code-style-guide.md`](../../agent-templates/code-style-guide.md)

## Quality gates de este servicio

Ejecutar antes de cada commit (vía `pnpm quality` o el agente `quality`):

- `pnpm typecheck`
- `pnpm lint` (Biome 2.4)
- `pnpm test` (Vitest)
- `pnpm test:e2e` (Playwright)
- WCAG 2.1 AA + 4 estados UI obligatorios
- SonarCloud quality gate: PASS

## Mantenimiento

Actualizar este archivo cuando cambien quality gates o el agente principal del servicio.
