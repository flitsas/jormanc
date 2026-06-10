# Planificación — Feature #8987 Habeas Data (enlace política de privacidad)

**Fecha:** 2026-05-26  
**Feature ADO:** [#8987](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8987)  
**HU hija:** [#8988](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8988) (2 SP, FRONTEND)

## Resumen ejecutivo

| Campo | Feature #8987 | HU #8988 |
|-------|---------------|----------|
| Tipo | Feature | User Story |
| Estado | Active | Active |
| Iteración | FLIT - EVOLUTION\Iteration 2 | Igual |
| Tags | `adopcion-ia`, `DOR` | `adopcion-ia`, `DOR` |
| Asignado | Juan Felipe Montoya Garcia | Igual |
| SP | — | 2 |

## Feature #8987 — Objetivo

Mejorar transparencia y cumplimiento normativo (Habeas Data) en el **paso 1** del wizard **Nuevo trámite**, convirtiendo el texto de aceptación en un **hipervínculo** a la política oficial sin afectar el estado del formulario.

**Módulo:** Trámites / Traspasos (Flit 2.0)  
**Archivo objetivo:** `frontend/src/features/traspasos/components/steps/StepConsultaVehiculo.tsx`  
**URL:** `https://flitsas.com.co/privacy-policy` (configurable vía `VITE_PRIVACY_POLICY_URL`)  
**Patrón de referencia:** enlace externo en `TramiteDetailHero.tsx` (`target="_blank"`, `rel="noopener noreferrer"`)

### Criterios funcionales (Feature)

1. Label del paso 1 incluye enlace interactivo a la política.
2. Texto del enlace: «Acepto la política de privacidad y tratamiento de datos (Habeas Data).»
3. URL apunta a `https://flitsas.com.co/privacy-policy`.
4. Nueva pestaña con `target="_blank"` y `rel="noopener noreferrer"`.
5. Clic en enlace abre URL; clic en checkbox marca/desmarca aceptación (controles independientes).

**Descomposición:** 1 US [FRONTEND] hija (#8988, 2 SP). Complejidad S.

## HU #8988 — Implementación

**Título:** `[US #8988] [FRONTEND] - Tramites - Enlace Habeas Data en paso 1 del wizard`  
**Dependencias:** Ninguna

### Escenarios Gherkin (AC)

| # | Escenario | Tipo |
|---|-----------|------|
| 1 | Enlace abre política en nueva pestaña | Positivo |
| 2 | Checkbox independiente del enlace | Positivo |
| 3 | Clic en enlace no marca checkbox | Borde |
| 4 | Consulta bloqueada sin aceptación | Negativo |
| 5 | Enlace seguro (`rel`, `target`) | Positivo |

### Estado actual del código (pre-implementación)

En `StepConsultaVehiculo.tsx` el texto Habeas Data es un `<label htmlFor="privacyAccepted">` plano — **sin enlace**. La HU describe el cambio esperado.

## Test Cases QA

Documento completo: [`docs/qa/tcs-8988-tramites-habeas-enlace.md`](../qa/tcs-8988-tramites-habeas-enlace.md)  
**8 TCs** en formato FLIT `QA_TC##_TRAMITES_HABEAS - {escenario}`, publicados como Tasks #8995–#9002 bajo HU #8988.

## Enlaces

- Diseño (referenciado en ADO): `docs/designs/8987-habeas-data-privacy-link.md` (pendiente en repo local si no existe)
- ADO export: `docs/reports/ado-wi-8987-8988.json`
