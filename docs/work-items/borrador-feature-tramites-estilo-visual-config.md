**ADO:** [#9304](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9304) — Estado: New — Asignado: samuel.cardenas@flitsas.com  
**Planificación (local):** [planificacion-9304-tramites-estilo-visual-config-2026-06-01.md](./planificacion-9304-tramites-estilo-visual-config-2026-06-01.md) — _Wiki ADO pendiente de permisos de escritura_  
**Creado:** 2026-06-01 — Skill: `@feature-creator` — Supervisor: Samuel Cardenas

Título: [TRÁMITES] - Reubicar Estilo Visual en configuración global del módulo

# OBJETIVO

Trasladar y reubicar la sección actual de **Estilo Visual** fuera de la interfaz principal del módulo de Trámites de FLIT, agrupándola en un nuevo flujo de **configuración global** accesible desde la barra superior. El objetivo es limpiar el panel principal del módulo (donde el usuario gestiona trámites activos) sin perder funcionalidad: los controles de estilo deben conservar la misma lógica, persistencia y comportamiento que la versión embebida, aplicarse de forma **global a la cuenta/sesión del usuario** y mantener **retrocompatibilidad** con preferencias ya guardadas (p. ej. `flit-home-style` en `localStorage`).

# DESCRIPTION

## Contexto

Hoy el selector de estilos visuales (presets *Clásico FLIT*, *Corporativo*, *Alto contraste*, etc.) se renderiza en la pantalla principal del módulo de trámites (dashboard), ocupando espacio en el flujo operativo. El negocio solicita separar esta capacidad en una vista dedicada de configuración, accesible sin abandonar el trámite en curso en la pestaña original.

Relación con trabajo previo: la capa de presets y persistencia local fue introducida en el Feature [#9237](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9237) (*Selección de estilos visuales en la página de inicio*). Este Feature **no redefine** el catálogo ni las reglas de los presets; **reubica la UI** y el punto de acceso dentro del módulo de Trámites.

## Especificaciones UI/UX

| Área | Comportamiento esperado |
|------|-------------------------|
| Remoción en panel principal | Eliminar por completo el bloque/sección **Estilo Visual** de la pantalla o panel principal del módulo de trámites (dashboard/listado). |
| Botón de ayuda `(?)` | En la barra superior derecha, el botón de interrogación debe abrir un **menú desplegable (dropdown)**. |
| Nueva opción de menú | El dropdown incluye la opción **Configuración**. |
| Apertura en nueva pestaña | Al hacer clic en **Configuración**, el sistema abre una **nueva pestaña del navegador** (`target="_blank"`, `rel="noopener noreferrer"`), sin navegar ni recargar la pestaña donde el usuario tiene un trámite activo. |
| Ruta dedicada | La nueva pestaña carga una **ruta dedicada** del frontend (definir en planificación; p. ej. `/traspasos/configuracion` o `/tramites/configuracion`). |
| Título de pestaña | El `<title>` del documento y la etiqueta visible de la pestaña del navegador debe ser **Estilo Visual y Configuraciones**. |
| Contenido de la vista | En esa vista se renderizan los **mismos controles** de estilo visual que existían en el panel principal, reutilizando `HomeStyleProvider`, `HomeStyleSelector`, hooks (`use-home-style`) y librería (`home-style` / clave `flit-home-style`). |
| Persistencia | Sin cambio de clave ni formato de almacenamiento salvo decisión explícita en HUs; los valores guardados previamente deben seguir aplicándose. |
| Alcance global | Los cambios realizados en la nueva pestaña deben reflejarse **globalmente** en la aplicación para ese usuario (misma sesión/navegador y, si aplica en fase posterior, preferencia de usuario en backend). |
| Estados UI | La nueva vista cumple convención FLIT: vacío, cargando, error y lleno. |
| Accesibilidad | WCAG 2.1 AA en dropdown, enlace de apertura y formulario de estilos. |

## Reglas de negocio

- El usuario no debe perder progreso ni contexto del trámite activo al acceder a configuración (por eso `_blank`).
- Un solo estilo activo por usuario en el contexto de presets de inicio/módulo (coherente con #9237).
- Cambiar estilo no invalida sesión, permisos ni datos del trámite.
- **Retrocompatibilidad obligatoria:** usuarios con `flit-home-style` (u otra clave vigente) ya persistida deben ver su selección aplicada al abrir la nueva vista y al volver al módulo principal.
- No exponer PII ni datos sensibles en metadatos de estilo.

## Entregables

- Frontend: eliminación de sección en dashboard; dropdown en `(?)`; ruta y página **Estilo Visual y Configuraciones**; pruebas unitarias y E2E actualizadas.
- QA: TCs de acceso por menú, apertura en nueva pestaña, persistencia, aplicación global y retrocompatibilidad.
- Planificación: descomposición en HUs (FRONTEND) bajo este Feature; wiki opcional vía `@planification-wiki`.

## Fuera de alcance (v1)

- Nuevos presets o editor visual de temas.
- Sincronización obligatoria con backend de preferencias de usuario (salvo HU explícita).
- Renombrar rutas API, carpetas `features/traspasos` o contratos de dominio de trámites.
- Configuraciones distintas a estilo visual en la misma vista (reservado para ampliaciones futuras del hub de configuración).

# CRITERIOS FUNCIONALES

- [ ] La pantalla principal del módulo de trámites **no muestra** la sección ni el componente de Estilo Visual.
- [ ] El botón `(?)` de la barra superior derecha despliega un menú con la opción **Configuración**.
- [ ] Al seleccionar **Configuración**, se abre una **nueva pestaña** del navegador sin descargar ni reemplazar la pestaña del trámite activo.
- [ ] La nueva pestaña carga la ruta dedicada y su título es **Estilo Visual y Configuraciones**.
- [ ] La nueva vista expone los controles de estilo con la **misma lógica y persistencia** que la versión anterior (incl. clave `flit-home-style` y atributo `data-home-style` en `<html>`).
- [ ] Los cambios de estilo en la nueva pestaña se **aplican globalmente** en la cuenta/sesión del usuario (visible al volver a la pestaña original y en otras vistas del módulo que consuman el preset).
- [ ] Usuarios con configuración de estilo **previamente guardada** conservan su selección sin migración manual (retrocompatibilidad).
- [ ] La UI de la nueva vista y del dropdown cumple **WCAG 2.1 AA** y los cuatro estados FLIT (vacío, cargando, error, lleno).
- [ ] No se introducen regresiones en el modo claro/oscuro existente ni en rutas/contratos API de trámites.
- [ ] Evidencia QA en DEV: flujo menú → nueva pestaña → cambio de preset → verificación en pestaña original y tras F5.
