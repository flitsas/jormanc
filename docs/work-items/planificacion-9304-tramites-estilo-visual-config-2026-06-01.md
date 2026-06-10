# Planificación: Reubicar Estilo Visual en configuración global del módulo

**Usuario:** Samuel Cardenas  
**Fecha:** 2026-06-01  
**Feature ADO:** [#9304](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9304)  
**Wiki (ruta objetivo):** `/Planificacion/9304-tramites-estilo-visual-config-2026-06-01`  
**URL esperada tras publicación:** https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_wiki/wikis/FLIT---EVOLUTION.wiki/Planificacion/9304-tramites-estilo-visual-config-2026-06-01

## Detalle de planificación

### Historias de Usuario (HUs)

| ID | Título | SP | Capa | Dependencias |
|----|--------|-----|------|--------------|
| [#9305](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9305) | [FRONTEND] – Trámites – Eliminar sección Estilo Visual del dashboard principal | 2 | Frontend | — |
| [#9306](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9306) | [FRONTEND] – Trámites – Agregar opción Configuración en menú de ayuda con nueva pestaña | 2 | Frontend | — |
| [#9307](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9307) | [FRONTEND] – Trámites – Implementar vista Estilo Visual y Configuraciones con persistencia global | 5 | Frontend | HU menú + ruta |
| [#9308](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9308) | [FRONTEND] – Trámites – Actualizar pruebas unitarias y E2E de reubicación de estilo visual | 2 | Frontend | HUs anteriores |

**Total Story Points:** 11 (Fibonacci). Sin HU BACKEND: persistencia en `localStorage` (`flit-home-style`); sin cambio de API.

**Feature relacionado:** [#9237](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9237) (catálogo de presets y lógica `home-style`).

### Diseños (UI/UX)

#### Flujo de usuario

```mermaid
flowchart LR
  A[Dashboard Trámites] --> B[Botón ? barra superior]
  B --> C[Dropdown: Soporte | Tickets | Configuración]
  C --> D[Nueva pestaña _blank]
  D --> E["/traspasos/configuracion"]
  E --> F[HomeStyleSelector + mismos presets]
  F --> G[localStorage flit-home-style]
  G --> H[Aplicación global data-home-style en html]
  A -.->|pestaña original sin recarga| A
```

#### Especificaciones

| Elemento | Decisión |
|----------|----------|
| Remoción | Quitar `<section>` Estilo visual de `TraspasosDashboardPage.tsx` (líneas ~54–66 actuales). |
| Menú `?` | Extender `HELP_MENU_ITEMS` en `DashboardLayout.tsx`: añadir **Configuración** con `target="_blank"`. Mantener Soporte y Consulta Tickets. |
| Ruta | **`/traspasos/configuracion`** (coherente con convención URL sin renombrar módulo). |
| Título pestaña | `document.title` = **Estilo Visual y Configuraciones** (p. ej. `useEffect` en página o util `setPageTitle`). |
| Vista destino | Página `TramitesConfigPage` bajo `features/traspasos/pages/`: card con `HomeStyleSelector`, copy alineado al feature (configuración global, no solo “página de inicio”). |
| Layout | Ruta **sin** sidebar operativo de trámite (layout mínimo o `DashboardLayout` solo header) para no distraer; `HomeStyleProvider` ya envuelve `App`. |
| Persistencia | Reutilizar `shared/lib/home-style.ts`, `use-home-style.tsx`, clave `flit-home-style`, atributo `data-home-style` en `<html>`. |
| Sincronización entre pestañas | Escuchar evento `storage` en `HomeStyleProvider` (si no existe) para reflejar cambios hechos en la pestaña de configuración sin F5. |
| Accesibilidad | Menú `role="menu"` existente; nuevo ítem con foco/roving tabindex; contraste WCAG 2.1 AA en selector. |
| Estados UI | Vacío (catálogo vacío improbable), cargando (hidratación inicial), error (localStorage bloqueado), lleno (selector visible). |

#### Fuera de alcance v1

- Nuevos presets, editor de temas, sync backend de preferencias.
- Otras secciones de configuración (notificaciones, idioma, etc.).

### Desarrollo Frontend

1. **`DashboardLayout.tsx`** — Añadir ítem `Configuración` → `/traspasos/configuracion`, `target="_blank"`, `rel="noopener noreferrer"`. Actualizar pruebas en `DashboardLayout.spec.ts`.
2. **`TraspasosDashboardPage.tsx`** — Eliminar sección Estilo visual e import de `HomeStyleSelector`. Ajustar `TraspasosDashboardPage.spec.tsx`.
3. **`TramitesConfigPage.tsx`** (nuevo) — Título H1 + `HomeStyleSelector`; `useEffect` para `document.title`; clases `flit-home-surface` / card existentes.
4. **`App.tsx`** — Ruta `GET /traspasos/configuracion` (shell ligero o página standalone).
5. **`use-home-style.tsx`** — Opcional: listener `storage` para aplicación global entre pestañas.
6. **E2E** — Extender o crear spec: menú `?` → Configuración → `page.context().waitForEvent('page')` o pestaña nueva; cambio preset; verificar `data-home-style` en pestaña original.
7. **Unitarios** — `home-style.spec.ts`, `use-home-style.spec.tsx`, `home-styles.spec.ts` (rutas y menú).

**Archivos de referencia actuales**

- `frontend/src/shared/components/ui/HomeStyleSelector.tsx`
- `frontend/src/shared/lib/home-style.ts` (`HOME_STYLE_STORAGE_KEY = 'flit-home-style'`)
- `frontend/src/styles/home-style.css`
- `frontend/e2e/home-styles.spec.ts`

**Restricciones**

- No renombrar `features/traspasos`, endpoints `/api/v1/traspasos/*` ni enums de dominio.
- No cambiar clave `flit-home-style` sin HU explícita de migración.

### Desarrollo Backend

**N/A** para v1. La preferencia de estilo es cliente (`localStorage`). Fase futura opcional: endpoint de preferencias de usuario si el PO lo prioriza.

### QA (smoke sugerido)

| TC | Escenario |
|----|-----------|
| P0 | Dashboard sin bloque Estilo visual |
| P0 | Menú `?` muestra Configuración y abre nueva pestaña |
| P0 | Título de pestaña = Estilo Visual y Configuraciones |
| P0 | Cambio de preset en config → visible en pestaña de trámites (storage / data-home-style) |
| P1 | Valor previo en `flit-home-style` se respeta (retrocompatibilidad) |
| P1 | F5 en ambas pestañas mantiene selección |

### Sprint sugerido

Asignar al **siguiente sprint al activo** en `FLIT - EVOLUTION` (convención FLIT regla #1).

### Referencias

- Feature padre: [#9304](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9304)
- Presets base: [#9237](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9237)
- Borrador local: `docs/work-items/borrador-feature-tramites-estilo-visual-config.md`

---

*Documento generado con la skill planification-wiki bajo supervisión de Samuel Cardenas.*
