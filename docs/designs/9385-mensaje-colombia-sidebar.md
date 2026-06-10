# Diseño: Mensaje tricolor Colombia en footer del sidebar

**Feature:** [#9385](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9385)  
**Arquitecto:** architecture-agent · supervisión Jorman Copete  
**Fecha:** 2026-06-02  
**Sprint objetivo:** `FLIT - EVOLUTION\Iteration 2`  
**ADR:** No requerido (cambio puramente visual, sin impacto arquitectónico)

---

## Contexto

El layout compartido `DashboardLayout` muestra en el footer del sidebar el enlace externo **BY FLIT**. Se solicita agregar debajo un mensaje estático *"Este año ganamos el mundial"* con los colores oficiales de la selección Colombia.

---

## Contrato API

**N/A** — Sin cambios backend.

---

## Alternativas evaluadas

### Opción 1 — Palabras coloreadas con `<span>` (recomendada)

**Mecanismo:** Dividir la frase en segmentos (`Este año` / `ganamos` / `el mundial`) y aplicar `text-[#FCD116]`, `text-[#003893]`, `text-[#CE1126]` vía Tailwind arbitrary values.

**Pros:** Simple, legible, sin imágenes, fácil de testear, buen contraste sobre fondo oscuro del sidebar.  
**Contras:** Colores arbitrarios fuera del design token (aceptable para mensaje promocional puntual).  
**Esfuerzo:** S  
**Riesgos:** Bajo.

### Opción 2 — Gradiente `background-clip: text`

**Pros:** Efecto visual continuo tricolor.  
**Contras:** Contraste variable; peor accesibilidad en fondos oscuros.  
**Esfuerzo:** S  
**Riesgos:** Medio (WCAG).

### Opción 3 — Imagen/SVG de bandera + texto

**Pros:** Fidelidad gráfica.  
**Contras:** Over-engineering; no escalable; accesibilidad compleja.  
**Esfuerzo:** M  
**Riesgos:** Alto.

**Decisión:** Opción 1.

---

## Cambios por capa

| Capa | Archivo | Cambio |
|------|---------|--------|
| Frontend | `frontend/src/shared/components/ui/DashboardLayout.tsx` | Bloque `<p>` debajo del enlace BY FLIT con spans coloreados |
| Tests | `frontend/src/shared/components/ui/DashboardLayout.spec.ts` | Assert texto visible y clases de color |

---

## Colores oficiales Colombia

| Color | Hex |
|-------|-----|
| Amarillo | `#FCD116` |
| Azul | `#003893` |
| Rojo | `#CE1126` |

---

## Notas QA

- Verificar en sidebar desktop y drawer mobile.
- Confirmar que BY FLIT sigue siendo enlace accesible.
- Sin regresión en tests existentes de HelpButton.
