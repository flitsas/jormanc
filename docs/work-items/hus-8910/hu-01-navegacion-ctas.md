# FRONTEND - Renombre navegación y CTAs principales del módulo Trámites

**Feature padre:** #8910  
**Story Points:** 2

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Nomenclatura Trámites en navegación y acciones principales

  Scenario: Menú lateral muestra Trámites
    Given un usuario autenticado en el módulo de procedimientos vehiculares
    When abre el menú lateral del módulo
    Then el ítem de listado se etiqueta "Trámites" y no "Traspasos"
    And el ítem de creación se etiqueta "Nuevo trámite" y no "Nuevo traspaso"

  Scenario: Menú administración enlaza al módulo como Trámites
    Given un usuario con acceso al menú de administración
    When visualiza el enlace al módulo vehicular
    Then el label visible es "Trámites"

  Scenario: Dashboard CTA crear trámite
    Given el usuario está en el listado de trámites (/traspasos)
    When observa el botón principal de creación
    Then el texto visible es "Nuevo trámite"

  Scenario: Breadcrumbs de regreso en wizard y detalle legacy
    Given el usuario está en el wizard o detalle con breadcrumb de regreso
    When visualiza el enlace al listado
    Then el texto visible es "Trámites"

  Scenario: Rutas y API sin cambios
    Given se aplicó el renombre de labels
    Then las rutas URL permanecen /traspasos y /traspasos/nuevo
    And las llamadas a /api/v1/traspasos/tramites no cambian
```

## Notas técnicas

- Archivos de referencia: `frontend/src/App.tsx`, `TraspasosDashboardPage.tsx`, `TraspasoNuevoPage.tsx`, `TraspasoDetailPage.tsx`, `TramiteDetailHero.tsx`.
- Entrega parcial en commit `7d2f729`; validar cobertura completa y accesibilidad (aria-label).
- Solo modificar strings visibles; no renombrar carpetas, hooks ni endpoints.
