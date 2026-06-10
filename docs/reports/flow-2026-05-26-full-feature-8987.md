# FULL_FEATURE — Feature #8987

**Flow:** `FULL_FEATURE`  
**Actualizado:** 2026-05-26  
**Feature:** [#8987](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8987) — **Active**  
**HU:** [#8988](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8988) — **Active**

---

## Resumen de etapas

| Etapa | Agente | Estado | Evidencia |
|-------|--------|--------|-----------|
| 0 DoR + Active | Tech Lead C | ✅ | Feature + HU en Active |
| 1 Diseño | Architecture | ✅ | `docs/designs/8987-habeas-data-privacy-link.md` |
| 2 Descomposición | Tech Lead B | ✅ | HU #8988 (2 SP) |
| 3 Implementación | Frontend | ✅ | Branch `agent/frontend/8988-habeas-data-privacy-link` commit `3092336` |
| 4 Review | Code Review + Security | ✅ | `docs/reports/pr-review-8988-habeas-data.md` — 0 bloqueantes |
| 5 Integración | Integration | ⏸️ **Checkpoint 4** | PR pendiente de abrir/mergear en GitHub |
| 6 Test Cases | QA A | ✅ | Tasks #8995–#9002 + `docs/qa/tcs-8988-tramites-habeas-enlace.md` |
| 7 Deploy DEV | Infra | ⏸️ | Post-merge a `develop` |

---

## Etapa 3 — PR

**Branch pusheada:** `agent/frontend/8988-habeas-data-privacy-link`  
**Abrir PR (manual — `gh` no disponible en entorno):**

https://github.com/flitsas/flit-boilerplate/compare/develop...agent/frontend/8988-habeas-data-privacy-link?expand=1

**Título sugerido:** `feat(tramites): enlace política Habeas Data paso 1 [#8988]`

**Verificación local previa a PR:**
- `npm run lint` ✅
- `npm test` ✅ (88 tests)
- `npm run typecheck` ✅
- Playwright TC enlace Habeas Data ✅

---

## Etapa 4 — Review Pipeline

**Veredicto:** ✅ LISTA PARA MERGE (inline security 0 bloqueantes)  
**Reporte:** `docs/reports/pr-review-8988-habeas-data.md`

---

## Etapa 5 — Integration Agent (9 pre-condiciones)

| # | Pre-condición | Estado |
|---|---------------|--------|
| 1 | PR active | ⏸️ Crear en GitHub |
| 2 | Branch convención | ✅ |
| 3 | Target develop | ✅ |
| 4 | ≥1 reviewer humano | ⏸️ |
| 5 | Code Review succeeded | ✅ (local) |
| 6 | Security succeeded | ✅ (inline) |
| 7 | Build CI succeeded | ⏸️ Post-PR |
| 8 | 0 threads activos | ⏸️ |
| 9 | US Refinement + SP | ✅ (2 SP, tag DOR) |

**Acción:** Tras abrir PR y CI verde → confirmación humana **«sí»** para merge squash.

**Commit message merge sugerido:**
```
feat(tramites): add Habeas Data privacy policy link on step 1 [#8988]

Co-authored-by: Cursor Agent <agent@cursor.com>
```

**Post-merge ADO:**
- HU #8988 → **Resolved**
- Comentario con SHA en `develop`
- Feature #8987 permanece **Active** hasta cierre PO

---

## Etapa 6 — Test Cases

| TC | Task ADO |
|----|----------|
| QA_TC01–08 | [#8995](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8995) – [#9002](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9002) |

---

## Etapa 7 — Deploy DEV (post-merge)

1. Pipeline GitHub Actions `develop` → build frontend con `VITE_PRIVACY_POLICY_URL`.
2. Infra Agent valida healthcheck DEV.
3. QA opcional: ejecutar TCs #8995–#9002 en DEV con `playwright-runner`.

---

## Checkpoint 4 — Humano (merge)

Confirma con **«sí»** cuando:
1. Hayas abierto la PR en el enlace anterior.
2. CI GitHub Actions esté verde.
3. Un reviewer humano haya aprobado.

Entonces ejecuto merge vía Integration Agent (squash → `develop`).

---

## Log

| Fecha | Evento |
|-------|--------|
| 2026-05-26 | DoR aprobado; Active #8987/#8988 |
| 2026-05-26 | Implementación + 8 TCs ADO |
| 2026-05-26 | Branch push + review PASS |
| 2026-05-26 | Comentarios ADO con enlace compare/PR |
