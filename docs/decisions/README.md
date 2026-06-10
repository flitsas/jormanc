# Architecture Decision Records — FLIT

Registro de decisiones arquitectónicas del proyecto.

## Convención

- Estado `Propuesto`: generado por el Architecture Agent o Claude Code (sesión interactiva). NO se aplica hasta promoverlo.
- Estado `Aceptado`: aprobado por el Líder Técnico humano en PR separada (regla FLIT 13).
- Estado `Superseded`: reemplazado por un ADR más reciente (referencia al nuevo).
- Estado `Rechazado`: propuesta evaluada y descartada (documentar razón).

## Índice

| ADR | Título | Estado |
|-----|--------|--------|
| [ADR-0001](ADR-0001-clean-architecture-backend.md) | Clean Architecture para backend | Aceptado |
| [ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md) | Consolidación del stack a .NET 10 + Python ML | **Aceptado 2026-05-27** |
| [ADR-0005](ADR-0005-pdf-in-process-questpdf.md) | Generación de PDF in-process con QuestPDF | **Aceptado 2026-05-27** |
| [ADR-0006](ADR-0006-gestion-archivos-vps-minio.md) | Gestión de archivos en VPS con MinIO + PostgreSQL | **Aceptado 2026-05-27** |
| [ADR-0007](ADR-0007-api-gateway-yarp.md) | API Gateway con YARP en .NET | **Aceptado 2026-05-27** |
| [ADR-0008](ADR-0008-database-agent-convenciones-persistencia.md) | Agente de BD y convenciones normativas de persistencia | **Propuesto 2026-06-02** |

> **Nota:** ADR-0003 (Feature-sliced architecture frontend) estaba listado pero el archivo nunca se creó. Se omite del índice hasta que exista.

> **Reset 2026-06-10:** al limpiar la implementación de trámites se eliminaron los ADR atados a ese dominio (0002 ClosedXML/export Excel, 0009 parametrización, 0010 snapshot al radicar, 0011 motor de reglas JSONB, 0012 OT cross-tenant). Se conservan los ADR transversales de arquitectura, stack e infra. La nueva implementación creará sus propios ADR a partir del correlativo siguiente.

## Bloque de ADRs portados desde repo hermano (2026-05-27)

| ADR | Origen | Notas |
|---|---|---|
| ADR-0004, 0005, 0006, 0007 | Portados desde repo hermano FLIT (`flit/`) donde estaban como ADR-0014/0015/0016/0017 | Renumerados al correlativo de este repo. Referencias internas a "ADR-0002 microservicios", "ADR-0004 BFF" y "ADR-0008 AWS S3" en el cuerpo de estos ADRs refieren al repo origen, NO a este repo. Cada ADR portado tiene una "Nota de port" al inicio aclarando esto. |

Este port aplicó un refactor integral al repo destino: eliminó `/backend/` (Node Fastify legacy), `/backend/dotnet/FLIT.Traspasos.sln` (.NET Layered), `/services/{core-api,go-gateway,node-bff}/` (scaffolds inertes). Trasplantó `services/core-api/` (Flit.slnx Modular Monolith con 19 proyectos), `infra/` (docker-compose + MinIO + Caddy), `.ai/` (sistema canónico de agentes IA), `frontend/` adaptaciones.

## Procedimiento para crear un ADR

```
Use the Architecture Agent to create an ADR for: <decisión>
```

O invocar el skill `flit-adr-generator`:

```
/flit:adr <decisión>
```

## Procedimiento para promover Propuesto → Aceptado

1. Crear branch `chore/promote-adr-NNNN`
2. Cambiar `**Estado:** Propuesto` → `**Estado:** Aceptado` en el ADR
3. Agregar `**Aceptado por:** <Nombre LT>` y `**Fecha de aceptación:** YYYY-MM-DD`
4. Si supersede otro ADR: marcar el anterior como `Superseded` con link al nuevo
5. Commit prefijo `ARCH: promote ADR-NNNN to Aceptado`
6. PR con aprobación del Líder Técnico humano

Cumple regla FLIT 13: solo el LT humano puede promover a Aceptado.
