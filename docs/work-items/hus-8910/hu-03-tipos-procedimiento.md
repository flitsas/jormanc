# FRONTEND - Etiquetas en español para tipos de procedimiento (matrícula inicial, traspaso)

**Feature padre:** #8910  
**Story Points:** 3

## Acceptance Criteria (Gherkin)

```gherkin
Feature: Tipos de procedimiento legibles en UI

  Scenario: Matrícula inicial sin enum crudo
    Given un trámite con tipo INITIAL_REGISTRATION o equivalente en respuesta API
    When el usuario ve el tipo en listado, detalle o confirmación
    Then el label visible es "Matrícula inicial" (o texto PO acordado)
    And no se muestra "INITIAL_REGISTRATION" ni valores técnicos crudos

  Scenario: Traspaso de dominio legible
    Given un trámite de transferencia de dominio
    When se muestra el tipo de procedimiento
    Then el usuario ve "Traspaso" o "Trámite de traspaso" según contexto de pantalla
    And los subtipos (bilateral, unilateral, transferencia dominio) mantienen etiquetas de negocio existentes

  Scenario: Consistencia en tabla y detalle
    Given el dashboard y la vista detalle del mismo trámite
    When se comparan las etiquetas de tipo
    Then usan el mismo mapa de traducción centralizado

  Scenario: Reglas RTM por fecha de matrícula intactas
    Given un vehículo con fecha de matrícula en consulta RUNT
    When se evalúan alertas RTM/SOAT
    Then la lógica de negocio no cambia por el renombre de labels
```

## Notas técnicas

- Crear o extender helper de labels (p. ej. `procedure-type-labels.ts`) consumido por `TramitesTable`, `TramiteDetailHero`, `StepConfirmacion`.
- Referencia enum: `procedure_type_enum` / `procedureType` en `traspasos.schemas.ts`.
- Sin cambios en contrato API ni backend .NET para este HU.
