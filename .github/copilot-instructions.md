# GitHub Copilot — Instrucciones del repositorio FLIT

> Instrucciones del repositorio FLIT. Editar directamente en este archivo.
> Agentes y skills en Cursor: `.cursor/agents/`, `.cursor/skills/`.

## Stack

- Backend: .NET 10 (core-api con Gateway YARP + Files MinIO + SignalR + RUNT + PDF QuestPDF), Python 3.13 (python-ml para OCR/ML)
- Frontend: Next.js 16 + React 19 + TypeScript + Tailwind 4 + Biome
- Calidad: Biome (frontend), `dotnet format`, Ruff, SonarCloud
- Gestión: Azure DevOps Boards (Feature → US → Task → Bug)
- Repo: pnpm workspaces (monorepo)

## Convenciones críticas (extracto de `agent-templates/conventions.md`)

- **Branches:** `feature/AB-1234-descripcion` (post-refactor 2026-05-22)
- **Commits:** `HU1234: descripción breve`
- PRs target: `develop` (nunca `main`), ≤ 800 líneas
- TypeScript strict (no `any`, no `as`, no `!`)
- 4 estados de UI: vacío / cargando / error / lleno
- WCAG 2.1 AA en todo frontend

## Agentes disponibles

Ver `.github/instructions/agent-*.instructions.md` para reglas específicas por scope.
Lista completa en `.ai/GUIA.md`.

## No-go zones

- `infra/secrets/` — secretos generados por `scripts/gen-secrets.sh`
- `.env`, `.env.user-identity`, `.env.verifik`
- `.ai/_legacy/` y `.ai/_generated/` (auto-generados)
- `docs/decisions/ADR-*.md` en estado `Aceptado` (solo Líder Técnico modifica)
