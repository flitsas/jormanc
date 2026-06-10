# FRONTEND - Actualizar pruebas E2E y unitarias por renombre UI Trámites

**Feature padre:** #8910  
**Story Points:** 3

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Suite de pruebas alineada a nomenclatura Trámites

  Scenario: E2E smoke navegación y CTA
    Given la suite Playwright en frontend/e2e/traspasos.spec.ts
    When se ejecuta npm run test:e2e desde el monorepo
    Then las aserciones de texto buscan "Trámites" y "Nuevo trámite"
    And no fallan por strings obsoletos "Traspasos" / "Nuevo traspaso" en elementos modificados

  Scenario: E2E subtipos sin regresión
    Given traspasos-subtipos.spec.ts
    When se ejecuta la suite de subtipos
    Then los flujos bilateral, unilateral y transferencia dominio pasan
    And los selectores por role/label siguen siendo estables

  Scenario: Unit tests Vitest del módulo
    Given npm run test -w frontend
    When se ejecutan specs en features/traspasos
    Then todos los tests pasan sin snapshots de texto obsoleto

  Scenario: Cobertura plan FLIT UI_RENAME
    Given los casos QA_TC del plan de renombre (módulo TRASPASOS)
    When QA ejecuta smoke P0 en DEV
    Then la evidencia referencia los textos nuevos acordados
```

## Notas técnicos

- Archivos: `frontend/e2e/traspasos.spec.ts`, `traspasos-subtipos.spec.ts`, `e2e/helpers/traspasos-wizard.ts`.
- Ejecutar desde raíz: `npm run test -w frontend` y `npm run test:e2e`.
- Rama de referencia: `feature/flit-traspaso-back-front`.
