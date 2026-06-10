# ADR-0010: Versionado de configuración por snapshot inmutable al radicar

**Fecha**: 2026-06-02
**Status**: Propuesto
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, datos, versionado, tramites, jsonb

---

## Contexto

Las reglas estándar (§5.3, §10.3) y los Features #9408/#9409 exigen que **un trámite radicado conserve
la versión de configuración con la que inició**: si Producto cambia la matriz, los campos, los
documentos o las reglas de un tipo de trámite, los trámites ya en curso/radicados **no deben verse
afectados**. La configuración efectiva resulta de resolver maestros globales + overrides tenant + OT
([ADR-0009](ADR-0009-modelo-parametrizacion-tramites.md)), por lo que "la versión" no es una sola
tabla sino un **grafo** de filas de config.

---

## Decisión

Adoptar **snapshot-on-file (copy-on-radicate)**: al iniciar el proceso formal del trámite, un
**resolver** computa la config efectiva (aristas activas, secciones/campos, documentos requeridos,
consultas y reglas aplicables) y la **congela** en `procedures.procedure_instances.config_snapshot`
(`jsonb`), con `config_schema_version`. La instancia opera contra ese snapshot y **no depende de FKs
vivas** hacia cada fila de config; mantiene solo el linaje (`procedure_type_id`, RESTRICT) para
trazabilidad. Las versiones de **plantillas** (`document_template_versions`) son inmutables una vez
publicadas, y el documento generado guarda `template_version_id`. Las reglas eliminadas no rompen
instancias porque el set aplicado vive en el snapshot.

El snapshot incluye config suficiente para **renderizar y validar offline** + *version pins*
(`template_version_id`, ids+hash de reglas). La PII se referencia por id donde sea posible (no se
incrustan datos sensibles) para permitir el derecho al olvido sin reescribir snapshots
([ADR-0012] y notas de seguridad).

---

## Alternativas consideradas

### Opción 1 — Snapshot inmutable al radicar (elegida)
**Pros:** runtime simple e inmune a ediciones; reproducible; sin joins a config viva al operar/
exportar; resiliente a borrado de config. **Contras:** duplica datos de config dentro de cada
instancia; cambios masivos retroactivos requieren reprocesar snapshots. **Esfuerzo:** M.

### Opción 2 — Config versionada + puntero de versión desde la instancia
**Pros:** normalizado, sin duplicar; trazabilidad por versión. **Contras:** exige versionar **todas**
las tablas de config (matriz, forms, docs, reglas) y nunca borrar versiones; el runtime hace joins a
N tablas versionadas; complejidad alta. **Esfuerzo:** L. **Riesgo:** integridad si se purga una
versión referenciada.

### Opción 3 — Puntero + snapshot (híbrido)
**Pros:** máxima trazabilidad (linaje + copia). **Contras:** mayor almacenamiento y dos fuentes de
verdad a conciliar. **Esfuerzo:** L. Se reserva como evolución si se requiere auditoría fina de "qué
versión exacta de cada tabla".

---

## Consecuencias

**Positivas:** cumple §5.3/§10.3 y #9408 FR-5; el motor de reglas puede eliminar/editar reglas sin
romper radicados (#9410 CF-B5); export/PDF reproducible.

**Negativas / mitigaciones:** definir un **JSON Schema del snapshot** + `config_schema_version` para
evolucionar el formato; jobs de reproceso opcionales para cambios retroactivos deliberados; vigilar
el tamaño del `jsonb` (pins + config resuelta, no logs).

**Relacionados:** [ADR-0009](ADR-0009-modelo-parametrizacion-tramites.md),
[ADR-0011](ADR-0011-motor-reglas-jsonb.md). DDL: `docs/designs/tramites-2.0/ddl/80-procedures.sql`.
