# [FRONTEND] – Traspasos – Actualizar pruebas del renombre CTA Crear traspaso

**ADO:** [#9267](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9267)  
**Feature padre:** [#9265](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9265)  
**Depende de:** [#9266](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9266)  
**Story Points:** 2

## Description

Como equipo de desarrollo del módulo Traspasos,  
quiero actualizar las pruebas E2E y unitarias que validan los textos de los CTAs de creación,  
para evitar regresiones y mantener la suite verde tras el renombre a "Crear traspaso".

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Pruebas del renombre CTA Crear traspaso

  Scenario: E2E dashboard y navegación
    Given la suite Playwright traspasos.spec.ts
    When se ejecutan los escenarios del dashboard y navegación
    Then las aserciones buscan "Crear traspaso" en lugar de "Nuevo trámite"
    And los escenarios pasan en ambiente DEV

  Scenario: E2E estado vacío
    Given el escenario de dashboard vacío con CTA crear
    When se ejecuta el test E2E correspondiente
    Then valida el texto "Crear traspaso" en el CTA
    And confirma navegación a /traspasos/nuevo

  Scenario: Regresión sin roturas de rutas
    Given los tests E2E de flujos de creación de borrador
    When se ejecuta la suite traspasos.spec.ts
    Then no hay fallos por rutas URL ni selectores de navegación rotos

  Scenario: Texto obsoleto no presente
    Given los tests actualizados
    When se busca explícitamente "Nuevo trámite" en CTAs de creación
    Then la aserción debe fallar (el texto ya no debe estar presente)
```

## Notas técnicas

- Depende de la HU #9266 (renombre UI).
- Archivos: `frontend/e2e/traspasos.spec.ts`
- Ejecutar `pnpm test:e2e` filtrando traspasos antes del PR.
