# HU #8932 — FRONTEND - Interfaz de generacion y visualizacion del consolidado PDF

**Feature padre:** #8930  
**SP:** 3 | **Estado ADO:** Active (iniciado 2026-05-26)  
**Depende de:** #8931

## Acceptance Criteria

**Scenario:** Generación de consolidado en curso  
**Given** que el usuario está en la vista de Expediente del Trámite  
**When** hace clic en el botón primario Generar Consolidado  
**Then** la interfaz debe mostrar un estado de Loading (Skeleton bloqueante)  
**And** realizar la petición al backend  

**Scenario:** Visualización exitosa  
**Given** que la petición al backend es exitosa  
**When** se recibe la respuesta con el documento  
**Then** la interfaz debe cambiar al estado Lleno  
**And** mostrar un visor embebido con el PDF final  

**Scenario:** Falla por documentos faltantes  
**Given** que la petición falla con error HTTP 400  
**When** se recibe la respuesta de error  
**Then** la interfaz debe mostrar un Modal de alerta dinámica  
**And** mapear y listar claramente los anexos faltantes  

## Notas técnicas (planificación)

- Componente propuesto: `TramiteConsolidadoPanel` en `TraspasoDetailPage`
- Ver planificación en `docs/work-items/planificacion-8930-consolidado-pdf-2026-05-26.md`
