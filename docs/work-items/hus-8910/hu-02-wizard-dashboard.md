# FRONTEND - Alinear títulos del wizard y dashboard (H1, continuar trámite)

**Feature padre:** #8910  
**Story Points:** 3

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Títulos coherentes Trámites en listado y wizard

  Scenario: H1 del listado alineado
    Given el usuario abre el dashboard de trámites
    When lee el encabezado principal de la página
    Then no aparece "Traspasos vehiculares" como título principal salvo decisión PO documentada
    And el encabezado usa nomenclatura acordada (p. ej. "Trámites vehiculares")

  Scenario: Título del wizard al crear
    Given el usuario inicia un trámite nuevo en /traspasos/nuevo
    When visualiza el encabezado del wizard
    Then el título visible es "Nuevo trámite" o equivalente acordado sin "Nuevo traspaso"

  Scenario: Título al continuar borrador
    Given existe un trámite en borrador
    When el usuario continúa el wizard
    Then el encabezado muestra "Continuar trámite" y no "Continuar traspaso"

  Scenario: Branding sidebar del layout
    Given el usuario navega dentro del shell del módulo
    When observa el branding del sidebar (DashboardLayout)
    Then la terminología es coherente con "Trámites" según decisión registrada en wiki #8910

  Scenario: Estado vacío coherente
    Given no hay trámites registrados
    When se muestra el empty state
    Then los textos de acción no invitan a "Crear traspaso"
```

## Notas técnicas

- Archivos: `TraspasosDashboardPage.tsx`, `TraspasoNuevoPage.tsx`, `TramitesEmptyState.tsx`, `DashboardLayout` (branding).
- Coordinar con PO si el subtítulo "Traspasos vehiculares" permanece como marca secundaria.
- Actualizar `aria-labelledby` / headings para WCAG 2.1 AA.
