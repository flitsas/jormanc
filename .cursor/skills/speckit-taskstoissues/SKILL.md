---
name: "speckit-taskstoissues"
description: "[FLIT override] Convert existing tasks into Azure DevOps User Stories (NOT GitHub issues) via the flit-spec-to-ado bridge. FLIT manages work items in Azure DevOps Boards."
compatibility: "Requires spec-kit project structure with .specify/ directory"
metadata:
  author: "github-spec-kit (FLIT override)"
  source: "templates/commands/taskstoissues.md"
---


## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

> [!CAUTION]
> **FLIT override.** En este repositorio la gestión de historias/features vive en **Azure DevOps Boards**, no en GitHub Issues. Este comando **NO** debe crear GitHub Issues bajo ninguna circunstancia. La ruta oficial es la skill **`flit-spec-to-ado`** (Modo B), que crea Historias de Usuario en ADO reusando `flit-crear-hu` y el contrato `flit-azure-devops`.

## Pre-Execution Checks

**Check for extension hooks (before tasks-to-issues conversion)**:
- Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.before_taskstoissues` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Pre-Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Pre-Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}

    Wait for the result of the hook command before proceeding to the Outline.
    ```
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently

## Outline (FLIT — ruta Azure DevOps)

1. Run `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` from repo root and parse FEATURE_DIR and AVAILABLE_DOCS list. All paths must be absolute.
2. Confirmar que existe `<FEATURE_DIR>/tasks.md` y, si aplica, `<FEATURE_DIR>/ado-link.json` con el `featureId` padre (generado por `flit-spec-to-ado` Modo A tras `/speckit-specify`). Si no existe el Feature padre en ADO, ejecutar primero `flit-spec-to-ado` Modo A.
3. **Invocar la skill `flit-spec-to-ado` (Modo B)**, que:
   - agrupa las tasks por capa (`[FRONTEND]` / `[BACKEND]`) en Historias de Usuario,
   - crea cada HU en Azure DevOps vía `flit-crear-hu` (narrativa Como/quiero/para, AC Gherkin, Story Points Fibonacci, vínculo al Feature padre),
   - respeta auth/encoding/idempotencia/WIQL de `flit-azure-devops`,
   - acumula los `userStoryIds` en `<FEATURE_DIR>/ado-link.json` y deja trazabilidad `[spec-kit]` en el Discussion.

> [!CAUTION]
> NO usar el MCP de GitHub para crear issues. NO crear issues en ningún repositorio. La única salida válida son Historias de Usuario en Azure DevOps (o, sin credenciales ADO, borradores `.md` locales según el fallback de `flit-azure-devops`).

## Post-Execution Checks

**Check for extension hooks (after tasks-to-issues conversion)**:
Check if `.specify/extensions.yml` exists in the project root.
- If it exists, read it and look for entries under the `hooks.after_taskstoissues` key
- If the YAML cannot be parsed or is invalid, skip hook checking silently and continue normally
- Filter out hooks where `enabled` is explicitly `false`. Treat hooks without an `enabled` field as enabled by default.
- For each remaining hook, do **not** attempt to interpret or evaluate hook `condition` expressions:
  - If the hook has no `condition` field, or it is null/empty, treat the hook as executable
  - If the hook defines a non-empty `condition`, skip the hook and leave condition evaluation to the HookExecutor implementation
- For each executable hook, output the following based on its `optional` flag:
  - **Optional hook** (`optional: true`):
    ```
    ## Extension Hooks

    **Optional Hook**: {extension}
    Command: `/{command}`
    Description: {description}

    Prompt: {prompt}
    To execute: `/{command}`
    ```
  - **Mandatory hook** (`optional: false`):
    ```
    ## Extension Hooks

    **Automatic Hook**: {extension}
    Executing: `/{command}`
    EXECUTE_COMMAND: {command}
    ```
- If no hooks are registered or `.specify/extensions.yml` does not exist, skip silently
