**ADO:** [#9237](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9237) — Estado: New — Asignado: abraham.canon@flitsas.com  
**Creado:** 2026-05-29 — Skill: `@feature-creator` — Supervisor: Abraham Enrique Cañon Vasquez

Título: [FRONTEND] - Selección de estilos visuales en la página de inicio

# OBJETIVO

Permitir que cada cliente (organización o tenant) elija entre **varios estilos visuales predefinidos** para la **página de inicio** de FLIT (landing o dashboard de bienvenida, según defina el PO), de modo que la experiencia refleje su identidad de marca sin desplegar código ni cambiar rutas. El estilo elegido debe aplicarse de forma consistente (paleta, tipografía, espaciado y componentes clave del hero/secciones de inicio) y **persistir** entre sesiones del usuario en ese contexto.

# DESCRIPTION

## Contexto

Hoy la aplicación soporta preferencia **claro / oscuro / sistema** (`flit-theme` en `localStorage`, tokens Tailwind `flit-*` y temas PrimeReact). Este Feature agrega una capa de **presets de estilo visual** (p. ej. *Clásico FLIT*, *Corporativo*, *Alto contraste*) orientados a la **página de inicio**, independientes o combinables con el modo claro/oscuro según diseño acordado.

## Alcance funcional

| Área | Comportamiento esperado |
|------|-------------------------|
| Selector de estilo | Control visible en la página de inicio (o panel de preferencias accesible desde ella) con **vista previa** de cada preset |
| Presets | Catálogo inicial acordado con PO/UX (mínimo **3** estilos); cada uno define tokens CSS/Tailwind o variables (colores primarios, fondo, tipografía, bordes/sombras) |
| Aplicación | Al seleccionar un estilo, la página de inicio se actualiza **sin recarga completa**; el resto de la app puede heredar el estilo o limitarse al alcance de inicio (decisión en planificación) |
| Persistencia | Guardar la elección en `localStorage` (clave dedicada, p. ej. `flit-home-style`) y, si aplica negocio, sincronizar con preferencias de usuario en backend |
| Accesibilidad | Cada preset debe cumplir **WCAG 2.1 AA** en contraste de texto y controles interactivos |
| Estados UI | Vacío (sin presets), cargando, error y lleno según convención FLIT |
| Multi-tenant (opcional fase 2) | Si el cliente viene identificado por tenant, **estilo por defecto** configurable por administrador |

## Reglas de negocio

- Solo se pueden elegir estilos del **catálogo publicado**; no edición libre de colores por el usuario final en v1.
- Un usuario tiene **un estilo activo** a la vez para la página de inicio.
- Cambiar de estilo no debe invalidar sesión ni permisos.
- Los estilos no deben exponer datos sensibles ni PII en nombres o metadatos.

## Entregables

- Frontend: componente selector, aplicación de tokens/preset en layout y secciones de inicio, persistencia local.
- Diseño: matriz de tokens por preset (Figma o wiki).
- QA: TCs smoke de selección, persistencia tras F5 y contraste por preset.
- Documentación wiki de planificación (tras aprobar Feature y crear ID en ADO).

## Fuera de alcance (v1)

- Editor visual drag-and-drop de temas.
- Estilos distintos por **cada** pantalla del módulo operativo (solo inicio salvo ampliación explícita).
- Renombrar rutas, APIs de trámites o lógica de negocio no relacionada con presentación.

# CRITERIOS FUNCIONALES

- [ ] El usuario ve en la página de inicio un selector con **al menos 3** estilos visuales predefinidos y nombre/descripción accesible de cada uno.
- [ ] Al elegir un estilo, la página de inicio refleja el cambio de forma inmediata (colores, tipografía y componentes acordados en el preset).
- [ ] La selección **persiste** tras cerrar el navegador y volver a entrar (misma clave de almacenamiento o preferencia de usuario).
- [ ] El estilo por defecto se aplica en la primera visita cuando no hay selección previa.
- [ ] El selector y el contenido cumplen **WCAG 2.1 AA** (contraste, foco visible, etiquetas ARIA).
- [ ] La UI maneja estados **cargando**, **error**, **vacío** (sin presets) y **lleno** según convención FLIT.
- [ ] El modo claro/oscuro existente sigue funcionando sin regresiones (comportamiento definido en wiki: combinable o independiente).
- [ ] No se alteran rutas URL ni contratos de API de dominio por el cambio de estilo.
- [ ] Evidencia QA en DEV con capturas por preset y prueba de persistencia.
