# Planificación: Estandarizar nomenclatura UI de Traspasos y Matrículas a Trámites vehiculares

**Usuario:** Hector Fabio Rivera Heurfano  
**Fecha:** 2026-05-25  
**Feature ADO:** [#8910](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8910)  
**Wiki:** [Planificación 2026-05-25](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_wiki/wikis/FLIT---EVOLUTION.wiki/8910-tramites-nomenclatura-ui-2026-05-25)

## Detalle de planificación

### Historias de Usuario (HUs)

| ID | Título | SP | Capa | Dependencias |
|----|--------|-----|------|--------------|
| [#8911](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8911) | FRONTEND - Renombre navegación y CTAs principales del módulo Trámites | 2 | Frontend | — (parcial en `7d2f729`) |
| [#8912](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8912) | FRONTEND - Alinear títulos del wizard y dashboard (H1, continuar trámite) | 3 | Frontend | #8911 |
| [#8913](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8913) | FRONTEND - Etiquetas en español para tipos de procedimiento (matrícula inicial, traspaso) | 3 | Frontend | — |
| [#8914](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8914) | FRONTEND - Actualizar pruebas E2E y unitarias por renombre UI Trámites | 3 | Frontend | #8911–8913 |

**Total Story Points:** 11 (Fibonacci). Sin HU BACKEND: el Feature es solo presentación UI; API y rutas permanecen bajo `/traspasos`.

### Diseños (UI/UX)

- **Principio:** terminología unificada **Trámites** / **Nuevo trámite** en navegación, CTAs y breadcrumbs.
- **Excepción permitida:** subtítulos de tipo de trámite (p. ej. "Traspaso bilateral") cuando describen el subtipo de negocio, no el módulo.
- **Pendiente PO:** branding sidebar "Traspasos vehiculares" vs "Trámites vehiculares".
- **Accesibilidad:** WCAG 2.1 AA — actualizar `aria-label`, headings y focus visible tras cambio de copy.
- **Rutas:** sin cambio (`/traspasos`, `/traspasos/nuevo`, `?tramiteId=&step=`).

### Desarrollo Frontend

1. **Navegación y CTAs** (`App.tsx`, dashboard, breadcrumbs, `TramiteDetailHero`).
2. **Wizard y listado** (`TraspasoNuevoPage`, `TraspasosDashboardPage`, empty state).
3. **Labels de tipo** — helper centralizado para `procedureType` / `INITIAL_REGISTRATION` → Matrícula inicial.
4. **Tests** — Playwright + Vitest; plan FLIT `UI_RENAME` (smoke P0).

**Restricción:** no renombrar `features/traspasos`, hooks, clientes API ni variables internas.

### Desarrollo Backend

**N/A** para este Feature. La API .NET permanece en `FLIT.Traspasos.*` y `/api/v1/traspasos/tramites`. Los tipos `INITIAL_REGISTRATION` y reglas RTM por fecha de matrícula no se modifican.

### Referencias

- Instrucción técnica: menú Traspasos → Trámites; botón → Nuevo trámite (solo labels).
- Rama: `feature/flit-traspaso-back-front`
- Reglas: `reglas-negocio-pasos-stepper.md` (enum `procedure_type_enum`)

---

*Documento generado con la skill planification-wiki bajo supervisión de Hector Fabio Rivera Heurfano.*
