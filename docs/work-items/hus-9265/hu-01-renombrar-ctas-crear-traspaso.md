# [FRONTEND] – Traspasos – Renombrar CTAs de creación a Crear traspaso

**ADO:** [#9266](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9266)  
**Feature padre:** [#9265](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9265)  
**Story Points:** 2

## Description

Como usuario del módulo de traspasos vehiculares,  
quiero ver la acción principal de creación etiquetada como "Crear traspaso" en el dashboard, menú y estado vacío,  
para que la interfaz refleje la terminología de negocio del traspaso de dominio sin confundirme con "Nuevo trámite".

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Renombre CTA Crear traspaso

  Scenario: Botón principal del dashboard
    Given un usuario autenticado en el listado de traspasos (/traspasos)
    When observa el botón principal de creación en el encabezado
    Then el texto visible es "Crear traspaso"
    And no se muestra "Nuevo trámite" ni "Crear trámite"

  Scenario: Ítem de menú de creación
    Given un usuario autenticado con el menú lateral del módulo visible
    When visualiza el ítem de acceso al flujo de creación
    Then el label visible es "Crear traspaso"
    And el enlace sigue apuntando a /traspasos/nuevo

  Scenario: CTA en estado vacío
    Given el listado de traspasos no tiene registros y no hay filtros activos
    When el usuario ve el estado vacío del catálogo
    Then el CTA principal muestra "Crear traspaso"
    And al activarlo navega a /traspasos/nuevo

  Scenario: Rutas y contratos sin cambios
    Given se aplicó el renombre de labels en la UI
    When el usuario usa los CTAs de creación
    Then las rutas URL permanecen /traspasos y /traspasos/nuevo
    And las llamadas a /api/v1/traspasos/tramites no cambian

  Scenario: Accesibilidad
    Given un lector de pantalla activo
    When el foco está en el botón o enlace de creación
    Then el nombre accesible incluye "Crear traspaso"
```

## Notas técnicas

- Archivos: `frontend/src/App.tsx`, `TraspasosDashboardPage.tsx`, `TramitesEmptyState.tsx`
- Solo modificar strings visibles y `aria-label`; no renombrar carpetas, hooks ni endpoints.
