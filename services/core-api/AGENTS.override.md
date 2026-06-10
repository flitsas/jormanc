# AGENTS.override.md — services/core-api

> Override por servicio para Codex/Junie. Editar directamente en este archivo.
> Override por servicio. **Codex** y **Junie** leen este archivo + `AGENTS.md` raíz cuando se invocan desde `services/core-api/`.

## Agente principal: `core-dotnet`

Agente principal para este servicio.

## Scope

`services/core-api/**`

## Lectura obligatoria al trabajar en este servicio

- [`AGENTS.md` raíz](../../AGENTS.md) — convenciones globales FLIT
- [`.ai/agents/core-dotnet.prompt.md`](../../.ai/agents/core-dotnet.prompt.md) — prompt completo del agente
- [`.ai/agents/core-dotnet.agent.yaml`](../../.ai/agents/core-dotnet.agent.yaml) — metadata, quality_gates, triggers
- [`agent-templates/conventions.md`](../../agent-templates/conventions.md) — 18 reglas innegociables
- [`agent-templates/code-style-guide.md`](../../agent-templates/code-style-guide.md)

## Quality gates de este servicio

Ejecutar antes de cada commit (vía `pnpm quality` o el agente `quality`):

- `dotnet format --severity error --verify-no-changes`
- `dotnet build -warnaserror`
- `dotnet test --no-build`
- NetArchTest verde (corre en los tests)
- SonarCloud quality gate: PASS

## Mantenimiento

Actualizar este archivo cuando cambien quality gates o el agente principal del servicio.
