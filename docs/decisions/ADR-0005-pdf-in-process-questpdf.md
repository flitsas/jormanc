# ADR-0005 — Generación de PDF in-process con QuestPDF en core-api

- **Estado:** **Aceptado**
- **Fecha:** 2026-05-27
- **Aceptado por:** Jorman Copete (Líder Técnico FLIT)
- **Fecha de aceptación:** 2026-05-27
- **Autor:** Claude Code (sesión interactiva con Líder Técnico)
- **Decisores:** Líder Técnico FLIT (Jorman Copete)
- **Consultados:** Architecture Agent, Backend .NET Agent, Infra Agent
- **Informados:** Equipo .NET, Equipo Frontend
- **Tags:** arquitectura, backend, .NET, pdf, performance, docker
- **Relaciona:** [ADR-0004 Consolidación stack](ADR-0004-consolidacion-stack-dotnet-python.md), [ADR-0006 Gestión archivos VPS](ADR-0006-gestion-archivos-vps-minio.md)

> **📦 Nota de port (2026-05-27):** ADR portado desde repo hermano FLIT. Decisión aplica igual aquí. Referencias a "ADR-0002 microservicios" en el cuerpo refieren al repo origen, no a este repo (donde ADR-0002 trata `closedxml-excel-export`).

---

## Contexto

FLIT genera PDFs en varios módulos del dominio: recibos de pago, certificados de trámites, constancias, RUT preformados, comprobantes de radicación, evidencias de firma. La generación es **side-effect del trámite** (no es el producto principal): el usuario completa un trámite y el sistema produce un PDF para descarga/auditoría.

El usuario hizo una prueba inicial con **Playwright** (renderizar HTML → PDF) y rechazó la imagen Docker resultante (~1.2-1.5 GB por incluir Chromium completo).

### Restricciones

| # | Restricción | Origen |
|---|---|---|
| C1 | Imagen Docker `core-api` debe quedar ≤ 200 MB (AOT) | ADR-0002 §5 + objetivo VPS |
| C2 | PDFs deben generarse async (no bloquear request HTTP) | Patrón Wolverine + Outbox |
| C3 | PDFs deben subirse automáticamente a object storage | ADR-0006 — MinIO |
| C4 | Soporte de PDF/A para archivado legal (Habeas Data y trámites colombianos) | Ley 594 de 2000, Decreto 2364 de 2012 |
| C5 | Firma digital sobre PDF (eventual) — compatibilidad con PAdES | RUNT/notarías futuro |
| C6 | AOT-compatible o con plan claro de JIT-fallback | ADR-0002 §5 |
| C7 | Licencia compatible con uso comercial sin obligación de open-source | FLIT producto comercial |

### Volumen estimado

- MVP: <50 PDFs/día (recibos + constancias de los trámites del piloto)
- Año 1: 5.000-20.000 PDFs/día (escala con adopción de trámites)
- PDFs típicos: 1-3 páginas, layout estructurado tipo factura/certificado, sin imágenes pesadas (ocasionalmente fotos de cédula)

→ **No es un caso de "rendering pesado de HTML"**, es un caso de "documento estructurado tipo formulario".

---

## Decisión propuesta

**Adoptar QuestPDF como librería única de generación de PDF en `services/core-api/`**, integrada en un nuevo proyecto `services/core-api/src/Flit.SharedKernel.Pdf/`.

**Patrón de uso:** generación **in-process, async, vía Wolverine handler**. Sin servicio dedicado.

### Diseño

```
[HTTP request: POST /api/procedures/{id}/receipt]
       │
       ▼
[Procedures handler] valida + publica evento "ReceiptRequested" → RabbitMQ
       │
       │ retorna 202 Accepted con receiptId
       ▼
[Wolverine consumer "ReceiptHandler" (thread pool, mismo proceso)]
       │
       ├─ resuelve datos del trámite (EF Core)
       ├─ invoca Flit.SharedKernel.Pdf.IPdfBuilder<ReceiptTemplate>
       ├─ QuestPDF genera stream PDF/A
       ├─ Sube a MinIO bucket "recibos" (Flit.Modules.Files)
       ├─ INSERT files.documents (status=ready, ref al receipt)
       ├─ UPDATE procedures.receipt_file_id
       └─ Publica evento "ReceiptGenerated" → SignalR notifica al frontend
```

### Estructura del proyecto

```
services/core-api/src/Flit.SharedKernel.Pdf/
├── Flit.SharedKernel.Pdf.csproj
├── IPdfBuilder.cs                          # interface genérica IPdfBuilder<TTemplate>
├── PdfBuilder.cs                           # implementación con QuestPDF
├── PdfBuilderOptions.cs                    # configuración (PDF/A profile, fonts, márgenes default)
├── ServiceCollectionExtensions.cs          # AddFlitPdf(this IServiceCollection)
├── Templates/
│   ├── ReceiptTemplate.cs                  # plantilla recibo de pago
│   ├── CertificateTemplate.cs              # plantilla certificado de trámite
│   ├── ProcedureRadicationTemplate.cs      # constancia de radicación
│   └── _Layouts/
│       ├── FlitDocumentLayout.cs           # header/footer FLIT corporativo
│       └── FlitColors.cs                   # paleta corporativa (cyan/blue/dark)
└── Fonts/
    └── InterVariable.ttf                   # font open-source embebido
```

### Dependencias NuGet

| Paquete | Versión | Notas |
|---|---|---|
| `QuestPDF` | 2026.x | Community License (gratuita <$1M USD/año revenue) — FLIT cualifica |
| `QuestPDF.PdfA` | 2026.x | Soporte PDF/A-1b y PDF/A-3b |

Las dos se agregan a `Directory.Packages.props` y se referencian solo desde `Flit.SharedKernel.Pdf`.

### Licencia QuestPDF

QuestPDF cambió a licencia dual desde v2024:
- **Community License** (gratis): organizaciones con revenue anual < $1M USD
- **Professional License**: arriba del threshold

FLIT cualifica como Community indefinidamente hasta cruzar el threshold. **Decisión:** registrar el uso en `Flit.SharedKernel.Pdf/README.md` y revisar anualmente. Si llega el día, contemplar paid license (~$899/año/desarrollador en 2026).

### Configuración AOT

QuestPDF es **100% C# puro** y **AOT-compatible** desde v2024. No requiere runtime nativo ni reflection unsafe. Se valida con un test `Flit.ArchTests.PdfAotTests` que confirma que la publicación AOT no genera warnings.

### Activación de license (boot)

```csharp
// Flit.Api/Program.cs
QuestPDF.Settings.License = LicenseType.Community;
```

### Ejemplo de uso (recibo)

```csharp
// Flit.SharedKernel.Pdf/Templates/ReceiptTemplate.cs
public sealed record ReceiptModel(
    string ProcedureCode,
    string CitizenName,
    string CitizenDocumentNumber,
    decimal Amount,
    DateTimeOffset PaidAt,
    IReadOnlyList<ReceiptLineItem> Items);

public sealed class ReceiptTemplate : IDocument
{
    private readonly ReceiptModel _m;
    public ReceiptTemplate(ReceiptModel m) => _m = m;

    public void Compose(IDocumentContainer c) => c.Page(p =>
    {
        p.Size(PageSizes.A4);
        p.Margin(2, Unit.Centimetre);
        p.DefaultTextStyle(t => t.FontFamily("Inter"));
        p.Header().Element(FlitDocumentLayout.Header);
        p.Content().Column(col =>
        {
            col.Item().Text($"Recibo de trámite {_m.ProcedureCode}").FontSize(16).Bold();
            col.Item().PaddingTop(10).Text($"Ciudadano: {_m.CitizenName} ({_m.CitizenDocumentNumber})");
            col.Item().PaddingTop(10).Element(BuildItemsTable);
            col.Item().PaddingTop(15).AlignRight().Text($"Total: ${_m.Amount:N0} COP").Bold();
        });
        p.Footer().Element(FlitDocumentLayout.Footer);
    });

    private void BuildItemsTable(IContainer c) { /* QuestPDF table fluent */ }
}
```

```csharp
// Wolverine handler
public sealed class ReceiptHandler
{
    public async Task Handle(
        ReceiptRequested cmd,
        IPdfBuilder<ReceiptTemplate> builder,
        IFileStorage storage,
        ProceduresDbContext db,
        CancellationToken ct)
    {
        var data = await db.Procedures.AsNoTracking()
            .Where(p => p.Id == cmd.ProcedureId)
            .Select(ReceiptModel.From).SingleAsync(ct);

        await using var pdf = await builder.BuildAsync(new ReceiptTemplate(data), ct);
        var fileId = await storage.UploadAsync(
            bucket: "recibos",
            keyPrefix: $"{cmd.ProcedureId}",
            contentType: "application/pdf",
            stream: pdf,
            ct: ct);

        await db.Procedures
            .Where(p => p.Id == cmd.ProcedureId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReceiptFileId, fileId), ct);
    }
}
```

---

## Alternativas consideradas

### Opción 1 — QuestPDF in-process (RECOMENDADA)

Descrita arriba.

**Pros:**
- API fluent moderna, productiva, idiomática C# 14
- 100% C# puro, AOT-compatible
- Imagen Docker no crece (~5 MB de NuGet añadido)
- Genera PDF/A-1b y PDF/A-3b nativamente
- Soporta firma digital con BouncyCastle (futuro PAdES)
- Mantenimiento activo, comunidad amplia
- Sin Chromium, sin runtime extra, sin shellout

**Cons:**
- Licencia Community con threshold ($1M/año) — riesgo futuro de pago
- No renderiza HTML/CSS arbitrario: requiere expresar las plantillas en código C#
- Curva inicial de la API fluent (~2 días para developer .NET)

**Esfuerzo:** S (proyecto + 3 plantillas iniciales = 1-2 días)
**Riesgos principales:** que aparezca un requisito tardío de "renderizar este HTML existente con CSS complejo" — mitigado por la naturaleza tipo formulario de los PDFs FLIT.

---

### Opción 2 — Servicio dedicado con Playwright/Chromium

Crear `services/pdf-renderer/` con Playwright .NET o Node + Puppeteer. Consume RabbitMQ, sin HTTP público.

**Pros:**
- Renderiza HTML/CSS pixel-perfect — útil si el equipo prefiere diseñar plantillas en HTML/Tailwind
- Frontend y PDF comparten el lenguaje de plantillas (HTML)
- Isolation: si OOM en el renderer, core-api sigue vivo

**Cons:**
- Imagen Docker ~1.2-1.5 GB (Chromium + libs)
- Cold start 2-4 seg por instancia
- RAM 150-300 MB por render concurrente
- Operación de un servicio adicional contradice ADR-0004 (consolidación)
- Sobre-ingeniería para 50-20.000 PDFs/día de layout estructurado
- Si se acepta este servicio, ADR-0004 §"Pros: 1 solo runtime" pierde validez

**Esfuerzo:** L (servicio nuevo + Dockerfile + observability + scaling + cola)
**Riesgos:** R1 (operativo)

---

### Opción 3 — PdfSharpCore + MigraDoc (MIT)

Librería MIT madura, sin licencia comercial restrictiva.

**Pros:**
- MIT, sin restricciones de revenue
- 100% .NET, AOT-compatible
- Imagen Docker no crece

**Cons:**
- API verbosa y poco ergonómica (estilo 2010), sin fluent
- Documentación limitada
- Comunidad pequeña, mantenimiento intermitente
- Plantillas muy verbosas comparadas con QuestPDF
- No soporta PDF/A out-of-the-box (requiere workarounds)

**Esfuerzo:** M (más código por plantilla, ~2-3x vs QuestPDF)
**Riesgos:** que el equipo termine reescribiendo a QuestPDF en 1 año por DX pobre.

---

## Tradeoff aceptado

Elegimos **Opción 1 (QuestPDF)** sobre las demás porque:

1. **Resuelve el rechazo explícito al Chromium** que motivó esta decisión
2. **Encaja con ADR-0004** (consolidación, sin nuevos servicios)
3. **Mejor DX y mejor calidad PDF/A** que PdfSharpCore para layouts tipo formulario
4. **Threshold de licencia es indolente para el horizonte previsible** (FLIT lejos de $1M/año)
5. **Soporta PDF/A y firma digital** que son requisitos futuros del dominio colombiano
6. **Cero impacto en imagen Docker** del core-api AOT

El **costo aceptado** es: (a) diseñar plantillas en C# en vez de HTML — mitigado con un layout corporativo reutilizable (`FlitDocumentLayout`); (b) potencial pago futuro de Professional License si la empresa cruza $1M/año (problema deseable).

---

## Comparativa

| Criterio | Op.1 QuestPDF | Op.2 Playwright | Op.3 PdfSharpCore |
|---|---|---|---|
| Imagen Docker delta | +5 MB | **+1.2 GB** | +3 MB |
| AOT-compatible | ✅ | ❌ | ✅ |
| API fluent moderna | ✅✅ | N/A (HTML) | ❌ |
| PDF/A nativo | ✅ | ⚠️ (Chrome plugin) | ❌ (workaround) |
| Firma digital PAdES | ✅ (BouncyCastle) | ⚠️ post-proceso | ⚠️ post-proceso |
| Licencia | Community <$1M | MIT (Playwright) | MIT |
| Costo VPS adicional | $0 | $20-40/mes (RAM) | $0 |
| Requiere servicio aparte | No | Sí | No |
| Velocidad render (3 págs) | ~50ms | ~500-1500ms | ~80ms |
| Curva aprendizaje | M (2 días) | S (HTML conocido) | L (API verbosa) |
| Cumple ADR-0004 consolidación | ✅ | ❌ | ✅ |

---

## Consecuencias positivas

- Imagen Docker core-api se mantiene ~200 MB AOT
- Generación en <100ms por PDF típico de 1-3 páginas
- Cero red entre dominio y generador (datos llegan ya hidratados al handler)
- Reutilización de objetos EF Core sin serializar entre servicios
- Plan claro de firma digital y PDF/A para roadmap legal

## Consecuencias negativas / riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| Q1 | Plantillas en C# obligan a developer a tocar código para cambio cosmético | Plantillas en `Flit.SharedKernel.Pdf/Templates/` con buen layout base; cambios menores son edits triviales |
| Q2 | Generación CPU-bound bloquea threads del pool si llegan muchas en simultáneo | Wolverine consume con `ThrottlingMiddleware` (`MaxDegreeOfParallelism = ProcessorCount`). Si insuficiente, extraer a servicio aparte (puerta abierta) |
| Q3 | Licencia Community puede cambiar términos en futuras versiones | Pinning de versión en `Directory.Packages.props`. Revisión anual de terms. |
| Q4 | Si surge requisito real de HTML→PDF (ej. cliente regulatorio exige reproducir un reporte web), QuestPDF no cubre | Aceptar entonces servicio aparte `pdf-renderer` solo para ese caso particular, sin migrar lo que ya funciona en QuestPDF |
| Q5 | Falta de fuentes embebidas puede producir PDF/A no válidos | Embeber `Inter Variable` (open-source, OFL) en `Flit.SharedKernel.Pdf/Fonts/`. Test ArchTest valida embed. |
| Q6 | Validación PDF/A en CI requiere herramienta externa | Job CI con `veraPDF` (Apache 2.0) que valida muestras generadas. Falla el pipeline si genera PDF/A inválido. |

---

## Plan de implementación (PR posterior a aprobación)

1. Crear `services/core-api/src/Flit.SharedKernel.Pdf/Flit.SharedKernel.Pdf.csproj`
2. Agregar `QuestPDF` y `QuestPDF.PdfA` a `Directory.Packages.props`
3. Implementar `IPdfBuilder<TTemplate>`, `PdfBuilder`, `PdfBuilderOptions`
4. Crear `FlitDocumentLayout` (header/footer + colores corporativos)
5. Crear 3 plantillas iniciales: `ReceiptTemplate`, `CertificateTemplate`, `ProcedureRadicationTemplate`
6. Embeber font `Inter Variable` (OFL)
7. Activar `QuestPDF.Settings.License = LicenseType.Community` en `Flit.Api/Program.cs`
8. Tests: `Flit.SharedKernel.Pdf.Tests/` con golden-file tests (compara con PDFs sample versionados)
9. ArchTest: `Flit.ArchTests/PdfAotTests.cs` valida AOT compatibility
10. CI job opcional: `veraPDF` valida muestras PDF/A
11. Documentar uso en `services/core-api/src/Flit.SharedKernel.Pdf/README.md`

**Estimado:** S (2-3 días)

---

## ADRs relacionados

- [ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md) — Consolidación del stack
- [ADR-0006](ADR-0006-gestion-archivos-vps-minio.md) — Gestión archivos VPS con MinIO (consumidor de los PDFs generados)
- [ADR-0002 §5](ADR-0002-arquitectura-microservicios-2026.md) — Stack tecnológico final

## Compliance

- **Habeas Data Ley 1581:** los PDFs con datos personales (cédula, RUT, fotos) se generan y suben a MinIO con SSE-S3 activado (ADR-0006). Audit log en `audit.data_access_log` cada vez que se genera o descarga.
- **Ley 594 de 2000 (archivo electrónico):** PDF/A-1b mínimo para documentos de archivo histórico.
- **Decreto 2364 de 2012 (firma electrónica):** plantillas preparadas para incorporar campo de firma PAdES en fase futura.

---

## Notas operativas para otros agentes

- **Backend Engineer .NET (`core-dotnet`):** propietario de `Flit.SharedKernel.Pdf`. Crea proyecto + 3 plantillas iniciales + tests golden-file.
- **Frontend Engineer:** sin cambios. Frontend descarga PDFs por presigned URL (ADR-0006) sin importar quién los generó.
- **QA Agent:** crear Test Cases para validar contenido y PDF/A compliance de cada plantilla. Usar `veraPDF` en pipeline.
- **Security Agent:** validar que los datos personales en plantillas se mascareen donde aplique (ej. cédula `1234XXX678`).
- **Infra Agent:** sin cambios. La imagen Docker core-api sigue siendo la misma AOT, solo agrega ~5 MB.

---

## Solicitud de aprobación humana

- [ ] Líder Técnico FLIT confirma elegir QuestPDF Community License (revisar términos anualmente)
- [ ] Líder Técnico FLIT confirma 2-3 días de scope inicial para el proyecto + 3 plantillas
- [ ] Líder Técnico FLIT promueve ADR-0005 a **Aceptado** junto con ADR-0004, ADR-0006, ADR-0007 en PR de promoción

## Trazabilidad

- **Entrada:** conversación 2026-05-27 con Líder Técnico, prueba fallida con Playwright (imagen Docker rechazada).
- **Salida (cuando se acepte):** PR `Flit.SharedKernel.Pdf` con scaffolding completo + tests.

---

*ADR generado por Claude Code (Opus 4.7) — 2026-05-27.*
*Estado:* **Aceptado** por el Líder Técnico humano el 2026-05-27 (regla FLIT 13 cumplida).
