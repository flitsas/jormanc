# Scripts SQL — Trámites 2.0 · schema `identity`

## Dónde están definidas las tablas de la HU #9415

Las tablas listadas en [ADO #9415](https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/9415) **ya están** en el DDL canónico:

| Tabla | Archivo | Líneas aprox. |
|---|---|---|
| `identity.roles` | `docs/designs/tramites-2.0/ddl/20-identity.sql` | 184–224 |
| `identity.role_permissions` | idem | 229–258 |
| `identity.user_roles` | idem | 263–291 |
| `identity.onboarding_invitations` | idem | 296–331 |
| `identity.password_reset_tokens` | idem | 336–363 |
| `identity.refresh_tokens` | idem | 368–397 |
| `identity.login_attempts` | idem | 438–453 |
| `identity.support_tickets` | idem | 458–489 |

**No** están en `scripts/sql/flit-shell-schema.sql` (modelo legacy shell: `rbac.*` + `identity.users` distinto). Para Trámites 2.0 usar el DDL de diseño.

## Cómo aplicar en Postgres local

```bash
pnpm docker:up:infra
pnpm db:tramites-identity
```

Equivale a ejecutar, en orden:

1. `docs/designs/tramites-2.0/ddl/00-extensions-and-schemas.sql` (extensiones, schemas, `audit.*`)
2. `docs/designs/tramites-2.0/ddl/20-identity.sql` (todas las tablas `identity` + seeds)

Scripts: `apply-identity.ps1` / `apply-identity.sh` en esta carpeta.

## Relación con HU #9412 (IDN-01)

La HU **#9412** aplica el mismo archivo `20-identity.sql` vía migración EF Core. **#9415** (login) consume:

- `identity.users`, `identity.refresh_tokens`, `identity.login_attempts` (escritura)
- `identity.user_roles`, `identity.role_permissions`, `identity.roles` (lectura de slugs)

Las tablas `onboarding_invitations`, `password_reset_tokens` y `support_tickets` pertenecen al mismo DDL y a otras HUs (IDN-05, IDN-08), no bloquean #9415.

## Schema `companies` (#9381 / #9383)

**DDL:** `docs/designs/tramites-2.0/ddl/30-companies.sql` (incluye `escrituras` y `escritura_attachments` del Feature #9383 en el mismo archivo).

```bash
pnpm db:tramites-companies
```

Matriz HU ↔ tabla: `docs/designs/tramites-2.0/06-matriz-tablas-companies.md`.

| Tabla | Feature |
|---|---|
| `companies`, `company_module_configs`, `signature_wallets`, `signature_wallet_movements`, `vehicle_ownership_rules` | #9381 (#9444–#9449) |
| `escrituras`, `escritura_attachments` | #9383 (#9450–#9452) |

## Conflicto con shell legacy

Si ya aplicaste `flit-shell-schema.sql`, coexisten dos modelos (`identity.users` shell vs `identity.users` tramites 2.0). Para desarrollo Trámites 2.0 se recomienda BD limpia:

```bash
pnpm docker:reset:infra
pnpm db:tramites-identity
```
