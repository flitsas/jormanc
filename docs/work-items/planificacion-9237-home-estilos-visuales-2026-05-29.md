# Planificación: Selección de estilos visuales en la página de inicio

**Usuario:** Abraham Enrique Cañon Vasquez  
**Fecha:** 2026-05-29  
**Feature ADO:** [#9237](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9237)

## Detalle de planificación

### Historias de Usuario (HUs)

| ID | Título | SP | Capa | Dependencias |
|----|--------|-----|------|--------------|
| [#9238](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9238) | [FRONTEND] – Home – Definir catálogo de presets y tokens visuales | 3 | Frontend | — |
| [#9239](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9239) | [FRONTEND] – Home – Implementar selector de estilos con vista previa | 5 | Frontend | #9238 |
| [#9240](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9240) | [FRONTEND] – Home – Aplicar preset y persistir selección en inicio | 3 | Frontend | #9239 |
| [#9241](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9241) | [FRONTEND] – Home – Pruebas automatizadas de estilos en página de inicio | 3 | Frontend | #9238–9240 |

**Total Story Points:** 14 (Fibonacci). Sin HU BACKEND en v1: persistencia en `localStorage`; sincronización API queda fuera de alcance.

### Diseños (UI/UX)

- **Presets iniciales (v1):** *Clásico FLIT* (actual), *Corporativo* (neutros, más contraste en headings), *Alto contraste* (WCAG reforzado).
- **Selector:** tarjetas o chips con miniatura de paleta + nombre + descripción breve; ubicación en página de inicio o panel accesible desde ella.
- **Vista previa:** al enfocar/hover o seleccionar, reflejar tokens en hero y secciones principales sin recarga.
- **Modo claro/oscuro:** combinable — cada preset define tokens para ambos modos (`html` / `html.dark`).
- **Accesibilidad:** WCAG 2.1 AA — contraste, foco visible, `aria-pressed` / `radiogroup` según patrón de control.
- **Estados FLIT:** vacío (sin presets), cargando, error, lleno.

### Desarrollo Frontend

1. **Tokens** — extender `tailwind.config.js` o CSS variables (`data-home-style="classic|corporate|high-contrast"`) en `index.css`; no romper tokens `flit-*` existentes.
2. **Hook/contexto** — `useHomeStyle` + clave `flit-home-style` en `localStorage`; estilo por defecto `classic`.
3. **Componentes** — `HomeStyleSelector`, integración en página de inicio (dashboard welcome o landing según PO).
4. **ThemeProvider** — validar que `use-theme` (light/dark/system) sigue operando; documentar matriz preset × modo.
5. **Tests** — Vitest (hook, persistencia); Playwright smoke (selección, F5, contraste básico).

**Restricción:** no cambiar rutas URL ni contratos API de dominio.

### Desarrollo Backend

**N/A** en v1. Fase 2 opcional: endpoint de preferencias de usuario/tenant para estilo por defecto.

### Referencias técnicas

- `frontend/src/shared/hooks/use-theme.tsx` — claro/oscuro/sistema
- `frontend/index.html` — bootstrap `flit-theme`
- `frontend/tailwind.config.js` — paleta `flit-*`

---

*Documento generado con la skill planification-wiki bajo supervisión de Abraham Enrique Cañon Vasquez.*
