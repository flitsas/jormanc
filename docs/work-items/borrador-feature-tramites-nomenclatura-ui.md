**ADO:** [#8910](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8910) — Estado: New — Asignado: hector.rivera@flitsas.com  
**Wiki:** [Planificación 2026-05-25](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_wiki/wikis/FLIT---EVOLUTION.wiki/8910-tramites-nomenclatura-ui-2026-05-25)  
**HUs:** [#8911](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8911) · [#8912](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8912) · [#8913](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8913) · [#8914](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8914)

Título: [TRASPASOS] - Estandarizar nomenclatura UI de Traspasos y Matrículas a Trámites vehiculares

# OBJETIVO

Alinear la experiencia de usuario del módulo de procedimientos vehiculares (traspaso de dominio, matrícula inicial y demás trámites del catálogo) con la terminología de negocio acordada: el usuario debe ver **Trámites** y **Nuevo trámite** en navegación y acciones principales, en lugar de **Traspasos** / **Crear traspaso**, sin alterar rutas URL, contratos de API ni identificadores internos del código. La primera entrega (commit `7d2f729` en rama `feature/flit-traspaso-back-front`) cubre menú, CTA del listado y breadcrumbs; este Feature formaliza el alcance completo, cierre de gaps pendientes y validación QA.

# DESCRIPTION

## Contexto (instrucción técnica aplicada)

1. Renombrar el menú principal de **"Traspasos"** a **"Trámites"**.
2. En la vista del módulo, cambiar el botón **"Nuevo traspaso"** / **"Crear traspaso"** por **"Nuevo trámite"**.
3. Modificar **únicamente etiquetas visibles** (UI/Labels); si existiera i18n, actualizar archivos de traducción.
4. Mantener intactos: lógica de negocio, nombres de funciones, endpoints API, variables internas, rutas URL (`/traspasos`, etc.) y nombres de archivos/carpetas del feature.

## Alcance funcional

| Área | Cambio esperado | Estado referencia |
|------|-----------------|-------------------|
| Menú lateral y admin | `Traspasos` → `Trámites`; `Nuevo traspaso` → `Nuevo trámite` | Implementado parcialmente |
| Subtítulo shell del módulo | `Traspasos` → `Trámites` | Implementado |
| Dashboard — botón crear | `Nuevo traspaso` → `Nuevo trámite` | Implementado |
| Breadcrumbs (wizard, detalle legacy) | `Traspasos` → `Trámites` | Implementado parcialmente |
| Título H1 listado (`Traspasos vehiculares`) | Evaluar alineación a **Trámites vehiculares** o equivalente acordado | Pendiente |
| Wizard (`Nuevo traspaso` / `Continuar traspaso`) | Alinear a **Nuevo trámite** / **Continuar trámite** | Pendiente |
| Detalle refactor (`TramiteDetailHero`) | Breadcrumb `Trámites` | Pendiente de commit conjunto |
| Branding sidebar (`Traspasos vehiculares`) | Definir si permanece marca o pasa a terminología unificada | Pendiente decisión PO |
| Tipos de procedimiento (enum) | En UI, traducir valores técnicos (`INITIAL_REGISTRATION` → **Matrícula inicial**, traspaso → **Traspaso** / **Trámite** según contexto); no exponer enums crudos | Regla FLIT frontend |
| Pruebas E2E y unitarias | Actualizar aserciones de texto visible acorde al renombre | Pendiente |

## Reglas de negocio y datos (sin cambio de contrato)

- La API sigue bajo `/api/v1/traspasos/tramites`.
- Tipos de procedimiento en backend incluyen traspaso y **matrícula inicial** (`INITIAL_REGISTRATION`); el renombre es de **presentación**, no de modelo de dominio.
- Reglas RTM/SOAT que dependen de **fecha de matrícula** del vehículo no se modifican en este Feature.

## Entregables

- Frontend: etiquetas UI consistentes en navegación, listado, wizard, detalle y estados vacíos/error.
- QA: casos smoke/regresión de renombre (plan FLIT módulo `TRASPASOS`, escenarios `UI_RENAME`).
- Documentación: actualizar plan de pruebas / wiki de planificación si aplica.

## Fuera de alcance

- Renombrar carpetas `features/traspasos`, proyectos .NET `FLIT.Traspasos.*` o rutas HTTP.
- Migración de base de datos o cambio de `procedure_type_enum`.
- Nuevos flujos de matrícula inicial (solo nomenclatura y coherencia con traspasos bajo **Trámites**).

# CRITERIOS FUNCIONALES

- [ ] El ítem de menú principal del módulo muestra **Trámites** (no "Traspasos") en navegación lateral y menú admin.
- [ ] El ítem secundario de creación muestra **Nuevo trámite** (no "Nuevo traspaso").
- [ ] El botón principal del listado/dashboard muestra **Nuevo trámite**.
- [ ] Los breadcrumbs de regreso desde wizard y detalle muestran **Trámites**.
- [ ] No se modifican rutas URL públicas (`/traspasos`, `/traspasos/nuevo`, query `tramiteId`, `step`).
- [ ] No se rompen llamadas a API ni identificadores TypeScript/C# internos por el cambio de labels.
- [ ] Títulos del wizard y H1 del listado quedan alineados con la nomenclatura acordada (sin mezclar "Traspaso" en UI visible salvo subtítulo de tipo de trámite, p. ej. "Traspaso bilateral").
- [ ] En pantallas que listan tipo de procedimiento, **Matrícula inicial** y traspaso se muestran en español legible (sin enums crudos).
- [ ] Tests E2E (`traspasos.spec.ts`, `traspasos-subtipos.spec.ts`) y unitarios relevantes pasan con las nuevas etiquetas.
- [ ] Revisión de accesibilidad: labels/aria no quedan desactualizados tras el renombre.
- [ ] Plan de pruebas FLIT ejecutado en DEV/QA con evidencia en work items hijos.
