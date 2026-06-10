# Planificación: Consolidado de documentos en PDF único (Trámites)

**Usuario:** Hector Fabio Rivera Huerfano  
**Fecha:** 2026-05-26  
**Feature ADO:** [#8930](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8930)  
**Wiki:** [8930-consolidado-pdf-tramites-2026-05-26](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_wiki/wikis/FLIT---EVOLUTION.wiki/8930-consolidado-pdf-tramites-2026-05-26)

## Detalle de planificación

### Historias de Usuario (HUs)

| ID | Título | SP | Capa | Dependencias |
|----|--------|-----|------|--------------|
| [#8931](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8931) | BACKEND - Motor de recoleccion y consolidacion de documentos PDF | 5 | Backend | — |
| [#8932](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8932) | FRONTEND - Interfaz de generacion y visualizacion del consolidado PDF | 3 | Frontend | #8931 (endpoint disponible) |

**Total Story Points:** 8 (Fibonacci). Orden de implementación: **8931 → 8932**.

### Alcance funcional

Generar un **PDF único** a partir del expediente de un trámite (`procedureId`), uniendo en orden fijo estos 10 anexos:

| # | Documento de negocio | Origen propuesto en código | Notas |
|---|----------------------|---------------------------|-------|
| 1 | Impronta | `procedure_documents` + verificaciones RUNT | Motor/chasis si aplica |
| 2 | Portada | Generación o plantilla (definir con LT) | Pendiente plantilla |
| 3 | Factura compraventa | `BUY_SELL` | Ya usado en API demo |
| 4 | FUR | `FurPdfService` / documento generado | Reutilizar generador FUR existente |
| 5 | Solicitud trámite virtual | `procedure_documents` | Confirmar `documentType` |
| 6 | Orden de compraventa | `procedure_documents` | Confirmar `documentType` |
| 7 | Paz y salvo impuestos | Party `paceAndSafe` o adjunto | Validar fuente |
| 8 | Documento identidad (comprador y vendedor) | Parties + adjuntos | Puede ser 2 PDFs → 2 páginas |
| 9 | Contrato privado de mandato | `procedure_documents` (mandante) | Condicional si hay mandante |
| 10 | Tablas certificadoras SOAT y RTM | Verificaciones / adjuntos | Confirmar `documentType` |

**Regla de negocio:** si falta **cualquier** documento obligatorio del listado → **HTTP 400** con lista de tipos faltantes (sin generar PDF parcial).

### Diseños (UI/UX)

- **Vista:** `TraspasoDetailPage` — sección “Expediente del trámite” (hero o sidebar existente).
- **CTA primario:** botón “Generar consolidado” (visible cuando trámite en estado apto: `Draft` finalizado o post-envío según decisión PO).
- **Estados UI (FLIT):**
  - Vacío: sin consolidado previo; CTA habilitado.
  - Cargando: skeleton bloqueante sobre el panel del visor.
  - Error 400: modal con lista de anexos faltantes (mapeo i18n de `documentType`).
  - Lleno: visor PDF embebido (`<iframe>` o componente accesible) + descarga.
- **Accesibilidad:** WCAG 2.1 AA — `aria-busy`, foco en modal de error, contraste en alertas.

### Desarrollo Backend (#8931)

**Stack:** .NET (`FLIT.Traspasos.*`), no Node para este módulo.

1. **Catálogo de tipos obligatorios** — enum/const `ConsolidatedDocumentSlot` con orden fijo y mapeo a `procedure_documents.document_type` + fuentes generadas (FUR).
2. **Use case** `GenerateTramiteConsolidatedPdf` en `Application/Tramites/`:
   - Cargar trámite + parties + documents + verificaciones vía `ITramiteService`.
   - Validar presencia de los 10 slots; acumular `missing[]`.
   - Descargar bytes desde `ITramiteFileStorage` (S3/local).
   - Normalizar a PDF (imágenes → PDF; PDF passthrough) con librería (evaluar **PdfSharp** / **QuestPDF** / **iText** — ADR si hay restricción de licencia).
   - Merge secuencial + compresión con límite de peso configurable (`ConsolidatedPdf:MaxSizeMb`).
   - Persistir resultado en storage + registrar fila en `procedure_documents` tipo `CONSOLIDATED_PACKAGE`.
3. **API:** `POST /api/v1/traspasos/tramites/{id}/consolidado` → `{ storageKey, fileName, fileSizeBytes, downloadUrl? }`.
4. **Errores:** `400 { missing: string[], message }` — mensaje en español.
5. **Tests:** unit (validación faltantes, orden merge); integration con storage mock.

**Dependencias existentes:**

- `TramitesController`, `procedure_documents`, `ITramiteFileStorage`, `FurPdfService`.
- Tabla `procedure_documents` ya indexada por `procedure_id`.

### Desarrollo Frontend (#8932)

1. **API client** en `features/traspasos/api/` — mutation `useGenerarConsolidado(tramiteId)`.
2. **Componente** `TramiteConsolidadoPanel` en `components/detail/`:
   - Integrar en `TraspasoDetailPage` o `TramiteDetailSections`.
   - 4 estados UI obligatorios.
   - Modal de error con lista de `missing` del backend.
3. **Schemas Zod** para respuesta éxito/error.
4. **i18n labels** — mapa `documentType` → etiqueta usuario (ej. `BUY_SELL` → “Factura de compraventa”).
5. **Tests:** Vitest (mapeo errores); Playwright smoke opcional en plan QA.

### Riesgos y decisiones pendientes (LT/PO)

| # | Tema | Impacto |
|---|------|---------|
| 1 | Códigos `documentType` definitivos para los 10 anexos | Bloquea validación backend |
| 2 | Librería PDF merge (licencia + calidad) | ADR Propuesto |
| 3 | ¿Consolidado solo post-paso 4 o también en borrador? | UX del CTA |
| 4 | Límite de peso del PDF final (MB) | Compresión |
| 5 | Mandante: ¿contrato obligatorio siempre o condicional? | Regla 400 |

### Plan de entrega

| Fase | Entregable | HU |
|------|------------|-----|
| 1 | Endpoint + validación faltantes + merge MVP | #8931 |
| 2 | UI generar/ver/descargar + errores | #8932 |
| 3 | TCs QA (`QA_TC##_TRAMITES_...`) | QA (post-Resolved) |

### Referencias

- Feature ADO descripción y AC Gherkin en work items 8930–8932.
- `implementation_plan.md` — carga documental paso 3, envío genera PDF (línea ~717).
- API actual: `POST .../tramites/{id}/documents` con `documentType: BUY_SELL`.

---

*Documento generado con la skill planification-wiki bajo supervisión de Hector Fabio Rivera Huerfano.*
