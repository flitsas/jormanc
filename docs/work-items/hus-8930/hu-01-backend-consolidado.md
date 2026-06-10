# HU #8931 — BACKEND - Motor de recoleccion y consolidacion de documentos PDF

**Feature padre:** #8930  
**SP:** 5 | **Estado ADO:** Active (iniciado 2026-05-26)

## Acceptance Criteria

**Scenario:** Generación exitosa del consolidado  
**Given** un trámite activo con ID válido  
**And** los 10 documentos obligatorios están presentes en el expediente  
**When** se invoca el servicio de generación de consolidado  
**Then** el sistema debe recolectar los 10 documentos  
**And** unirlos secuencialmente en un único archivo PDF  
**And** asociar el PDF resultante al trámite  
**And** retornar la ruta o identificador del nuevo documento  

**Scenario:** Documento faltante en el expediente  
**Given** un trámite activo con ID válido  
**And** falta uno o más de los 10 documentos obligatorios  
**When** se invoca el servicio de generación de consolidado  
**Then** el sistema debe abortar el proceso  
**And** retornar un error HTTP 400 detallando qué documentos faltan  

## Notas técnicas (planificación)

- Ver `docs/work-items/planificacion-8930-consolidado-pdf-2026-05-26.md`
- Endpoint propuesto: `POST /api/v1/traspasos/tramites/{id}/consolidado`
