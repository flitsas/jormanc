# Planificación: Renombrar botón crear trámite a Crear traspaso

**Usuario:** Willyn Londoño Calle  
**Fecha:** 2026-05-29  
**Feature ADO:** [#9265](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9265)

## Detalle de planificación

### Historias de Usuario (HUs)

| ID | Título | SP | Capa | Dependencias |
|----|--------|-----|------|--------------|
| [#9266](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9266) | [FRONTEND] – Traspasos – Renombrar CTAs de creación a Crear traspaso | 2 | Frontend | — |
| [#9267](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9267) | [FRONTEND] – Traspasos – Actualizar pruebas del renombre CTA Crear traspaso | 2 | Frontend | #9266 |

**Total Story Points:** 4 (Fibonacci). Sin HU BACKEND: cambio exclusivo de labels UI y pruebas.

### Desarrollo Frontend

1. **Dashboard** — `TraspasosDashboardPage.tsx`: botón `Nuevo trámite` → `Crear traspaso`
2. **Navegación** — `App.tsx`: ítem menú `Nuevo trámite` → `Crear traspaso`
3. **Estado vacío** — `TramitesEmptyState.tsx`: CTA `Crear el primero` → `Crear traspaso`
4. **Accesibilidad** — revisar `aria-label` en botones/enlaces de creación
5. **Tests** — actualizar `frontend/e2e/traspasos.spec.ts` (HU #9267)

**Restricción:** no cambiar rutas URL (`/traspasos`, `/traspasos/nuevo`) ni contratos API.

### Desarrollo Backend

**N/A**

### Sprint asignado

`FLIT - EVOLUTION\Iteration 2` (siguiente al sprint activo)

---

*Documento generado con la skill @flit-crear-hu bajo supervisión de Willyn Londoño Calle.*
