# Diseño Técnico — Feature #8983: Exportar total de trámites a Excel

**Estado:** Aprobado  
**Feature:** [#8983](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8983)  
**HUs:** [#8985 BACKEND](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8985) · [#8986 FRONTEND](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/8986)  
**Fecha:** 2026-05-26  
**Autor:** Architecture Agent (FLIT)

---

## Contexto

El módulo de Trámites / Traspasos (Flit 2.0) muestra un indicador `Total` en la barra de filtros con el conteo de trámites que coinciden con los filtros activos. Los usuarios necesitan exportar ese conjunto completo a Excel para análisis offline, sin limitarse a la página visible de la tabla.

## Decisión

**Opción A — Export en servidor, descarga de blob en cliente** ✓ *Seleccionada*

El backend genera el archivo `.xlsx` completo y lo devuelve como respuesta binaria. El frontend recibe el blob y lo descarga usando un `object URL`.

| Criterio | Opción A (server) | Opción B (client) |
|---|---|---|
| Datos completos sin paginación | ✓ | Requiere múltiples requests |
| Control de límite de filas | ✓ Trivial | Complejo en cliente |
| Riesgo de PII en logs | Controlado (no se loguea contenido) | Mayor exposición |
| Complejidad implementación | Baja-Media | Alta |

**ADR relacionado:** [ADR-0002-closedxml-excel-export.md](../decisions/ADR-0002-closedxml-excel-export.md) — uso de ClosedXML.

---

## Arquitectura

```
Frontend (React)                    Backend .NET 9
───────────────                     ──────────────
TramitesListPanel                   TramitesController
 └─ TramitesFilters                  └─ GET /api/v1/traspasos/tramites/export
     └─ [botón Total]                    └─ ITramiteService.ExportDashboardAsync()
                                              └─ TramiteService (infra)
         ──── GET /export?... ────►              ├─ ApplyFilters() [reutiliza lógica ListDashboard]
         ◄── blob .xlsx ─────────                ├─ TramiteExcelExporter.Build()
                                                  └─ TramiteExportOptions (MaxRows)
```

---

## Archivos a crear / modificar

### Backend (.NET)

| Archivo | Acción | Descripción |
|---|---|---|
| `Application/Tramites/ITramiteService.cs` | Modificar | Agregar `ExportDashboardAsync` |
| `Application/Tramites/TramiteDtos.cs` | Modificar | Agregar `TramiteExportResult`, excepciones |
| `Application/Tramites/TramiteExportOptions.cs` | **Nuevo** | Config `MaxRows` vía IOptions |
| `Infrastructure/Excel/TramiteExcelExporter.cs` | **Nuevo** | Generación .xlsx con ClosedXML |
| `Infrastructure/Services/TramiteService.cs` | Modificar | Implementar `ExportDashboardAsync` |
| `Infrastructure/DependencyInjection.cs` | Modificar | Registrar `TramiteExportOptions` |
| `Infrastructure/FLIT.Traspasos.Infrastructure.csproj` | Modificar | Agregar ClosedXML NuGet |
| `Api/Controllers/TramitesController.cs` | Modificar | Agregar `GET /export` |
| `Api/appsettings.json` | Modificar | Sección `TramiteExport.MaxRows` |

### Frontend (React)

| Archivo | Acción | Descripción |
|---|---|---|
| `features/traspasos/components/TramitesListPanel.tsx` | Modificar | Props `onExport`, `exporting`, `exportDisabled` |
| `features/traspasos/api/traspasos.api.ts` | Modificar | `exportTramitesExcel(params)` con `responseType: blob` |
| `features/traspasos/utils/export-tramites-excel.ts` | **Nuevo** | Helper descarga via object URL |

---

## Contrato API

```
GET /api/v1/traspasos/tramites/export

Query params (todos opcionales, mismos que listado):
  status  string   ProcedureStatus (Draft | Validated | ...)
  search  string   Texto libre
  company string   CompanyRegistered

Respuestas:
  200  application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
       Content-Disposition: attachment; filename="tramites-{yyyy-MM-dd}.xlsx"
       Body: bytes del archivo Excel

  422  application/json  → Límite de filas excedido
       { "message": "...", "totalCount": N, "maxRows": M }

  422  application/json  → Sin resultados
       { "message": "No hay trámites que coincidan con los filtros para exportar." }

  401  Unauthorized  → Sin autenticación
```

---

## Columnas del Excel (orden exacto)

| # | Encabezado | Campo fuente | Tipo |
|---|---|---|---|
| 1 | id trámite | `CompositeId` | string |
| 2 | trámite | `ProcedureName` | string |
| 3 | propietario | `OwnerName` | string |
| 4 | compañía | `CompanyName` | string |
| 5 | creado | `CreatedAt` | datetime `yyyy-MM-dd HH:mm` |
| 6 | modificado | `UpdatedAt` | datetime `yyyy-MM-dd HH:mm` |
| 7 | secretaria | `TrafficSecretary` | string |
| 8 | estado | `Status` → etiqueta en español | string |

**Etiquetas de estado en español:**

| Enum | Etiqueta |
|---|---|
| Draft | Borrador |
| WaitingValidation | En validación |
| Validated | Validado |
| Signed | Firmado |
| SentToOt | Enviado a OT |
| Approved | Aprobado |
| Rejected | Rechazado |
| Paused | Pausado |

---

## Configuración

```json
// appsettings.json
"TramiteExport": {
  "MaxRows": 5000
}
```

`MaxRows` es configurable por ambiente vía variable de entorno `TramiteExport__MaxRows`.

---

## Consideraciones de seguridad / Habeas Data

- **No loguear** el contenido del archivo ni datos de las columnas en ningún nivel de log.
- El archivo incluye `propietario` (PII). El endpoint debe estar protegido con autenticación (Bearer).
- Los bytes del Excel no deben cachearse en memoria más allá del request.
- No escribir el archivo en disco; generarlo en `MemoryStream` y descartar tras la respuesta.

---

## Complejidad y Story Points

- Backend: 5 SP (M)
- Frontend: 3 SP (S)
- Total: 8 SP
