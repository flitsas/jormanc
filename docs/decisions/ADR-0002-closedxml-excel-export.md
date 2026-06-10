# ADR-0002 — Librería para generación de archivos Excel (.xlsx) en backend .NET

**Estado:** Propuesto  
**Fecha:** 2026-05-26  
**Contexto:** Feature #8983 — Exportar total de trámites a Excel  
**Decidido por:** Architecture Agent (pendiente aceptación del Líder Técnico)

---

## Contexto

El Feature #8983 requiere que el backend .NET 9 genere archivos `.xlsx` para exportar el listado de trámites. Se necesita una librería que:

1. Sea compatible con .NET 9
2. Produzca archivos `.xlsx` válidos (OOXML)
3. Permita estilar encabezados (negrita, color)
4. Sea libre de dependencia de Excel instalado en servidor
5. Tenga mantenimiento activo y licencia comercialmente permisiva

---

## Alternativas evaluadas

### Opción A — ClosedXML (0.104.x) ✓ *Propuesta*

- **Licencia:** MIT
- **Compatibilidad:** .NET Standard 2.0+ / .NET 9 ✓
- **API:** Fluent, orientada a celdas (`ws.Cell(row, col).Value = ...`)
- **Mantenimiento:** Activo (último release < 6 meses)
- **Dependencias:** DocumentFormat.OpenXml (transitivo, Microsoft)
- **Tamaño artefacto:** +~2 MB al deploy
- **Pros:** API simple, bien documentada, amplio uso en proyectos .NET en producción
- **Contras:** Agrega una dependencia de terceros; consume más memoria que Open XML SDK directo

### Opción B — Open XML SDK (DocumentFormat.OpenXml, Microsoft)

- **Licencia:** MIT (Microsoft)
- **Compatibilidad:** .NET 9 ✓
- **API:** Verbosa — requiere manipular el XML subyacente directamente
- **Mantenimiento:** Activo (mantenido por Microsoft)
- **Pros:** Sin dependencias adicionales de terceros; ya transitivo si se usa ClosedXML
- **Contras:** API extremadamente verbosa para uso simple; mayor tiempo de desarrollo; riesgo de errores de formato OOXML

### Opción C — EPPlus (7.x)

- **Licencia:** Polyform Noncommercial License 1.0 (uso comercial **requiere licencia pagada**)
- **Compatibilidad:** .NET 9 ✓
- **Pros:** API fluente, buen soporte de gráficas
- **Contras:** **Licencia no permisiva para uso comercial** — descartada

---

## Decisión

**Opción A — ClosedXML** por su combinación de API simple, licencia MIT y compatibilidad con .NET 9.

La Opción C se descarta por restricción de licencia comercial. La Opción B requeriría significativamente más código para lograr el mismo resultado, aumentando el riesgo de defectos en el formato del archivo.

---

## Consecuencias

- Se agrega `ClosedXML` a `FLIT.Traspasos.Infrastructure.csproj`.
- El artefacto de deploy aumenta ~2 MB.
- El uso de ClosedXML queda encapsulado en `TramiteExcelExporter` (Infrastructure layer); si en el futuro se requiere cambiar la librería, solo ese archivo se modifica.
- Los archivos se generan en `MemoryStream` y nunca se persisten en disco.

---

## Revisión

Este ADR debe ser aceptado por el Líder Técnico antes de hacer merge del PR de la HU #8985.
