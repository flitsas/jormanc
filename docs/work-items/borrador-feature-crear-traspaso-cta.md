**ADO:** [#9265](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9265) — Estado: New — Asignado: willyn.londono@flitsas.com  
**Creado:** 2026-05-29 — Skill: `@feature-creator` — Supervisor: Willyn Londoño Calle  
**Planificación:** [planificacion-9265-crear-traspaso-cta-2026-05-29.md](./planificacion-9265-crear-traspaso-cta-2026-05-29.md)  
**HUs:** [#9266](./hus-9265/hu-01-renombrar-ctas-crear-traspaso.md) · [#9267](./hus-9265/hu-02-actualizar-pruebas-cta.md)

Título: [TRASPASOS] - Renombrar botón crear trámite a Crear traspaso

# OBJETIVO

Alinear el texto del botón principal de creación en el módulo de traspasos vehiculares con la terminología de negocio acordada: el usuario debe ver **Crear traspaso** en lugar de **Nuevo trámite** (o equivalentes como "Crear trámite") en el dashboard y CTAs relacionados, sin alterar rutas URL, contratos de API ni identificadores internos del código.

# DESCRIPTION

## Contexto

El Feature #8910 estandarizó la nomenclatura hacia "Trámites" y "Nuevo trámite". Este Feature revierte únicamente la etiqueta del **botón de acción principal de creación** hacia **Crear traspaso**, manteniendo coherencia con el dominio de negocio (traspaso de dominio vehicular).

## Alcance funcional

| Área | Texto actual | Texto objetivo |
|------|--------------|----------------|
| Dashboard — botón principal | Nuevo trámite | Crear traspaso |
| Menú lateral — ítem de creación | Nuevo trámite | Crear traspaso |
| Estado vacío — CTA | Crear el primero | Evaluar alineación (p. ej. Crear traspaso) |
| Tests E2E y unitarios | Aserciones con Nuevo trámite | Actualizar a Crear traspaso |

## Reglas

- Solo modificar **etiquetas visibles** (UI/labels, aria-label si aplica).
- Mantener intactos: rutas URL (`/traspasos`, `/traspasos/nuevo`), endpoints API, nombres de funciones, variables y carpetas del feature.
- El menú de listado puede permanecer como "Trámites" salvo decisión contraria del PO.

## Fuera de alcance

- Renombrar carpetas `features/traspasos`, proyectos .NET o rutas HTTP.
- Cambios de modelo de dominio o base de datos.

# CRITERIOS FUNCIONALES

- [ ] El botón principal del listado/dashboard muestra **Crear traspaso** (no "Nuevo trámite").
- [ ] El ítem de menú de creación muestra **Crear traspaso** (no "Nuevo trámite").
- [ ] No se modifican rutas URL públicas (`/traspasos`, `/traspasos/nuevo`).
- [ ] No se rompen llamadas a API ni identificadores TypeScript/C# internos.
- [ ] Tests E2E (`traspasos.spec.ts`) y unitarios relevantes pasan con las nuevas etiquetas.
- [ ] Labels y aria-labels quedan accesibles y alineados con el nuevo texto (WCAG 2.1 AA).
