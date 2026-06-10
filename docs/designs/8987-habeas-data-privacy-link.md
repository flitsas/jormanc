# Diseño — Feature #8987 Enlace política Habeas Data (paso 1)

**Estado:** Propuesto  
**Feature:** #8987  
**HU:** #8988  
**Módulo:** Trámites / Traspasos  
**Complejidad:** S

## Contexto

El paso 1 del wizard (`StepConsultaVehiculo.tsx`) exige aceptación Habeas Data antes de consultar RUNT. El texto debe enlazar a la política oficial sin perder el estado del formulario.

## Decisión

**Checkbox y enlace independientes** (sin `htmlFor` del label sobre el texto del enlace).

| Alternativa | Pros | Contras | Decisión |
|-------------|------|---------|----------|
| A. Label único con `htmlFor` | Menos markup | Clic en enlace marca checkbox | Rechazada |
| B. Checkbox + `<a>` separados | Cumple AC Gherkin; patrón FUR | Dos focos teclado | **Elegida** |
| C. Modal in-app con política | Sin salir del wizard | Fuera de alcance; mantenimiento legal | Rechazada |

## Implementación

- **URL:** `resolvePrivacyPolicyUrl()` lee `VITE_PRIVACY_POLICY_URL`, fallback `https://flitsas.com.co/privacy-policy`.
- **Seguridad:** `target="_blank"` + `rel="noopener noreferrer"` (mismo patrón que `TramiteDetailHero.tsx`).
- **A11y:** `aria-label` en checkbox con texto de aceptación; enlace con texto visible.
- **Tests:** unit (`resolve-privacy-policy-url.spec.ts`), E2E (`traspasos.spec.ts`).

## Archivos

| Archivo | Cambio |
|---------|--------|
| `frontend/src/shared/lib/resolve-privacy-policy-url.ts` | Nuevo helper env |
| `frontend/src/features/traspasos/components/steps/StepConsultaVehiculo.tsx` | UI checkbox + enlace |
| `frontend/.env.example` | Documenta `VITE_PRIVACY_POLICY_URL` |
| `frontend/e2e/*` | Helper + TC enlace |

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| Popup bloqueado | Enlace estándar; no depende de `window.open` |
| URL incorrecta en prod | Variable `VITE_*` en pipeline de build |
| Regresión E2E paso 1 | Helper usa `getByRole('checkbox')` |
