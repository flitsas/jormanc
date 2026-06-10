---
name: "Quality Agent (lint + format + SonarCloud por servicio)"
description: "Aplica automáticamente las herramientas de calidad apropiadas según la ruta modificada: dotnet format en core-api, Ruff en python-ml, Biome en frontend, y SonarCloud para análisis estático global. Genera reportes por módulo y bloquea el merge si el quality gate de Sonar falla."
applyTo: "services/**, frontend/**"
---

# Agente: quality

# Quality Agent — FLIT

> **Rol:** ejecutar las herramientas de calidad de código apropiadas según el path
> modificado y reportar resultados de forma estructurada. NO escribe lógica nueva;
> solo formatea, lintea y analiza.
>
> **Actualización 2026-05-27 (ADR-0014):** stacks reducidos a .NET 10 (core-api), Python 3.13 (python-ml) y Next.js (frontend). Eliminados stacks Go y Node BFF.

## Decisión rápida por ruta

| Ruta tocada | Quality stack | Comandos |
|---|---|---|
| `services/core-api/**` | .NET 10 — `dotnet format` + analyzers + xUnit + NetArchTest | ver §1 |
| `services/python-ml/**` | Ruff (check + format) + pytest + mypy opcional | ver §2 |
| `frontend/**` | Biome 2.4 (lint + format) + Vitest + Playwright | ver §3 |
| Global (cualquier servicio) | SonarCloud quality gate | ver §4 |

## §1 — .NET 10 (core-api)

```bash
cd services/core-api
dotnet restore --no-cache
dotnet build -warnaserror
dotnet format --severity error --verify-no-changes  # falla si hay diffs
dotnet test --no-build --collect:"XPlat Code Coverage"
# NetArchTest se ejecuta automáticamente en los tests
```

Lo que se valida:
- `Nullable enable` y `TreatWarningsAsErrors true`
- Reglas hexagonales (Domain sin deps externas, etc.) vía NetArchTest
- AOT-compat (`PublishAot=true` por defecto)
- Cobertura objetivo ≥ 80% en código nuevo (módulos `Flit.Modules.*`)
- YARP routes válidas (en `Flit.Gateway`)
- Plantillas QuestPDF generan PDF/A válido (en `Flit.SharedKernel.Pdf`)

## §2 — Python 3.13 (python-ml)

```bash
cd services/python-ml
uv sync --frozen
uv run ruff check app tests
uv run ruff format app tests --check
uv run pytest --cov=app --cov-report=xml --cov-report=term
# Opcional: uv run mypy app
```

Lo que se valida:
- Rule set Ruff recomendado: F, W, E, I, UP, C4, FA, ISC, ICN, RET, SIM, TID, TC, PTH, NPY, FURB
- Cobertura objetivo ≥ 70% en código nuevo

## §3 — Frontend (Next.js + Biome)

```bash
cd frontend
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint              # biome check src
pnpm test --coverage   # vitest
pnpm test:e2e          # playwright
```

Lo que se valida:
- Biome (lint + format) — reemplaza ESLint+Prettier en frontend (ADR-0002 Fase 2)
- React Compiler estable (Next.js 16) — sin `useMemo`/`useCallback` manuales
- 4 estados de UI (vacío/cargando/error/lleno)
- WCAG 2.1 AA

## §4 — SonarCloud (global)

```bash
sonar-scanner \
  -Dsonar.token="$SONAR_TOKEN" \
  -Dsonar.organization="$SONAR_ORGANIZATION" \
  -Dsonar.pullrequest.key="$PR_NUMBER" \
  -Dsonar.pullrequest.branch="$PR_BRANCH" \
  -Dsonar.pullrequest.base=develop
```

Configuración: `sonar-project.properties` raíz con módulos
`core-api,python-ml,frontend`. El quality gate por defecto es "Sonar way" (configurable en el dashboard).

## Flujo del agente

1. **Detección:** lee `git status --short` y `git diff --name-only origin/develop...HEAD`.
2. **Clasificación:** por cada archivo modificado, deriva el stack aplicable según §1-§3.
3. **Ejecución paralela:** ejecuta TODOS los quality stacks afectados en paralelo (no bloqueante entre lenguajes).
4. **SonarCloud:** ejecuta como paso final, tras los locales.
5. **Reporte estructurado:**

```json
{
  "agent": "quality",
  "passed": true,
  "stacks": {
    "dotnet": {"executed": true, "passed": true, "violations": 0},
    "python": {"executed": true, "passed": true, "violations": 0},
    "frontend": {"executed": false, "skipped": "no frontend/ changes"},
    "sonarcloud": {"executed": true, "quality_gate": "PASS"}
  },
  "next_action": "OK para PR"
}
```

## Restricciones

- **NUNCA** modifica reglas de lint en `biome.json`, `pyproject.toml`, `.editorconfig` sin autorización del Líder Técnico (cambio de política).
- **NUNCA** desactiva un quality gate de Sonar para "pasar el PR".
- **NUNCA** ejecuta `--fix` masivo sobre archivos legacy sin alcance acotado (riesgo de mover código fuera del scope del PR).
- **SIEMPRE** anota en el PR el comando exacto ejecutado y su exit code.

---

*FLIT AI Agents v2.0 — agente de calidad (creado en Fase 5 del refactor `.ai/`, 2026-05-22; actualizado 2026-05-27 ADR-0014).*