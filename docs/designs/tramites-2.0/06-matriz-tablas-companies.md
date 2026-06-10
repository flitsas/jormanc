# Matriz tablas `companies` · DDL 30 vs HUs ADO

**DDL canónico:** `docs/designs/tramites-2.0/ddl/30-companies.sql`  
**Aplicar local:** `pnpm db:tramites-companies` (requiere `ddl/00` + `ddl/10` + `ddl/20` + `ddl/25` previos)

## Regla de reparto Features

| Feature | Alcance tablas `companies` |
|---|---|
| **#9381** Compañías B2B | `companies`, `company_module_configs`, `signature_wallets`, `signature_wallet_movements`, `vehicle_ownership_rules` |
| **#9383** Escrituras | `escrituras`, `escritura_attachments` (+ `files.files` en ddl/25) |

`escrituras` y `escritura_attachments` **viven en el mismo archivo SQL** que #9381 por orden de despliegue (una sola migración `ddl/30`), pero **no** se consumen en las HUs #9444–#9449; su HU es **#9450–#9452**.

## Matriz HU ↔ tabla (✅ = referenciada en AC de la HU)

| Tabla | Schema | #9444 CMP-01 | #9445 | #9446 | #9447 | #9448 | #9449 | #9450 ESC-01 | #9451 | 30-companies.sql | ejemplos-datos.html |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `companies` | companies | ✅ Crear | ✅ | ✅ | — | ✅ | ✅ | — | ✅ | ✅ | ✅ |
| `company_module_configs` | companies | ✅ Crear | — | ✅ | ✅ | — | ✅ | — | — | ✅ | ✅ |
| `signature_wallets` | companies | ✅ Crear | — | ✅¹ | — | — | ✅ | — | — | ✅ | ✅ |
| `signature_wallet_movements` | companies | ✅ Crear | — | — | — | — | —² | — | — | ✅ | ✅³ |
| `vehicle_ownership_rules` | companies | ✅ Crear | — | — | — | ✅ | — | — | — | ✅ | ✅³ |
| `escrituras` | companies | —⁴ | — | — | — | — | — | ✅ Crear | ✅ | ✅ | ✅³ |
| `escritura_attachments` | companies | —⁴ | — | — | — | — | — | ✅ Crear | ✅ | ✅ | ✅³ |
| `runt_sync_log` | integrations | — | — | — | ✅ | — | — | — | — | ddl/70 | ✅* |
| `procedure_query_results` | procedures | — | — | — | ✅ | — | — | — | — | ddl/80 | ✅* |
| `identity.tenants` | identity | — | ✅ FK | — | — | — | — | — | — | ddl/20 | ✅* |

¹ Pestaña Empresa: saldo firmas. ² Consumo en runtime al firmar trámite (no UI #9449). ³ Tras corrección 2026-06-03. ⁴ Correcto que no aparezcan en #9444–#9449 (pertenecen a #9383). *Tabla en otro schema, ejemplo en sección relacionada.

## Errores corregidos (2026-06-03)

1. **ejemplos-datos.html** — Faltaban `signature_wallet_movements`, `vehicle_ownership_rules`, `escritura_attachments`; columnas de `escrituras` alineadas al DDL (`document_type_id`, `company_name`).
2. **Documentación** — Esta matriz y comentario inventario en cabecera de `30-companies.sql`.
3. **ADO #9444** — Nota: `ddl/30` incluye también tablas #9383; consumo en #9450+.
4. **ADO #9446** — Tabla `signature_wallets` en AC (pestaña Empresa).
