# Runbook: Despliegue en Producción — flit-boilerplate

**Proyecto:** flit-boilerplate
**Stack:** .NET 10 (Flit.Gateway YARP + Flit.Api) · React 19 + Vite + nginx (frontend) · PostgreSQL 16
**Monorepo:** pnpm (frontend) + .NET (`services/core-api`)
**Infraestructura:** Hostinger VPS · Ubuntu 24.04 · Docker Compose · GitHub Actions (CD)
**Rama que despliega:** `develop` (rama por defecto)
**Última actualización:** 2026-05-30

> **Nota:** el login está **deshabilitado temporalmente** en todos los ambientes
> (ver `Flit.Gateway/Program.cs`). No se requieren llaves JWT. El usuario se
> identifica con el header `X-User-Id` (UUID demo inyectado por el frontend).

---

## Índice

1. [Arquitectura de producción](#1-arquitectura-de-producción)
2. [Prerrequisitos](#2-prerrequisitos)
3. [Configuración inicial del VPS (una sola vez)](#3-configuración-inicial-del-vps-una-sola-vez)
4. [Configuración de PostgreSQL](#4-configuración-de-postgresql)
5. [Configuración del firewall (UFW)](#5-configuración-del-firewall-ufw)
6. [Configuración de nginx y SSL](#6-configuración-de-nginx-y-ssl)
7. [Configuración de GitHub Secrets](#7-configuración-de-github-secrets)
8. [Variables de entorno del VPS](#8-variables-de-entorno-del-vps)
9. [Primer despliegue](#9-primer-despliegue)
10. [Migraciones de base de datos](#10-migraciones-de-base-de-datos)
11. [Verificación del sistema](#11-verificación-del-sistema)
12. [Despliegues automáticos (CD)](#12-despliegues-automáticos-cd)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Arquitectura de producción

```
Internet
   │
   ▼
nginx (host, 80/443, SSL por certbot)
   ├── orca.flitsas.com       → 127.0.0.1:4001  (contenedor frontend / nginx+SPA)
   ├── api.orca.flitsas.com   → 127.0.0.1:4002  (contenedor gateway · Flit.Gateway YARP)
   └── core.orca.flitsas.com  → 127.0.0.1:4003  (contenedor core-api · Flit.Api) [opcional/debug]

Docker Compose (proyecto: flit-prod · red flit-prod_default)
   ├── frontend   → 127.0.0.1:4001 → :80   (React/Vite servido por nginx, solo estáticos)
   ├── gateway    → 127.0.0.1:4002 → :8080  (dotnet /app/gateway/Flit.Gateway.dll)
   │     └── YARP /api,/hubs → http://core-api:8081 (red interna Docker)
   └── core-api   → 127.0.0.1:4003 → :8081  (dotnet /app/api/Flit.Api.dll)
         │
         │ host.docker.internal (172.17.0.1) + extra_hosts host-gateway
         ▼
PostgreSQL 16 (en el HOST · 5432)  ·  DB: flit_dev  OWNER: flit
```

**Puntos clave de esta arquitectura (esquema de puertos 4xxx, ver `docs/designs/port-allocation-flit.md`):**

- **Una sola imagen** `ghcr.io/flitsas/flit-boilerplate/core-api` corre **dos** servicios; cada uno elige su DLL con `command` y su `working_dir` (necesario para que .NET encuentre su `appsettings.json`):
  - `gateway`  → `dotnet /app/gateway/Flit.Gateway.dll`, `working_dir: /app/gateway`
  - `core-api` → `dotnet /app/api/Flit.Api.dll`, `working_dir: /app/api`
- El **frontend** llama a la API por **URL absoluta** (`https://api.orca.flitsas.com/...`), no por proxy de nginx. Esas URLs se **hornean en el bundle al hacer build** (build-args), no en runtime.
- **PostgreSQL corre en el HOST**, no en Docker. Los contenedores lo alcanzan vía `host.docker.internal`.
- **El reverse-proxy del host** (nginx) termina TLS y enruta los 3 dominios a `127.0.0.1:4001/4002/4003`.

**Flujo de CD:** push a `develop` → GitHub Actions → build imágenes (`core-api`, `frontend`) → push a GHCR → SSH al VPS → `scp docker-compose.prod.yml` → `docker compose pull` + `up -d`.

---

## 2. Prerrequisitos

### En el VPS
- Ubuntu 24.04 LTS
- Docker Engine (`docker --version`) + Docker Compose v2 (`docker compose version`)
- nginx (`nginx -v`) + Certbot (`certbot --version`)
- PostgreSQL 16 instalado **en el host**

### En GitHub
- GitHub Actions habilitado e imágenes en GHCR (`ghcr.io/flitsas/flit-boilerplate/*`)
- Secrets configurados (ver §7)

### DNS (registros A → IP del VPS)
- `orca.flitsas.com` (frontend)
- `api.orca.flitsas.com` (gateway / API pública)
- `core.orca.flitsas.com` (Flit.Api directo — **opcional**, solo si quieres debug público)

---

## 3. Configuración inicial del VPS (una sola vez)

### 3.1 Docker, nginx, Certbot

```bash
curl -fsSL https://get.docker.com | sh
systemctl enable --now docker

apt update && apt install -y nginx certbot python3-certbot-nginx
systemctl enable --now nginx
```

### 3.2 Directorio de despliegue

```bash
mkdir -p /opt/flit-boilerplate/secrets   # secrets/ queda por compatibilidad; con login OFF no se usa
```

### 3.3 SSH key para GitHub Actions

En tu máquina local (Windows PowerShell):

```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.ssh"
ssh-keygen -t ed25519 -C "github-actions-flit" -f "$env:USERPROFILE\.ssh\github_actions_flit" -N '""'
```

En el VPS, autorizar la clave pública:

```bash
echo "ssh-ed25519 AAAA... github-actions-flit" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

---

## 4. Configuración de PostgreSQL

> El backend **.NET nuevo lee `ConnectionStrings:Core`** (formato Npgsql), **no** `DATABASE_URL`.
> Las migraciones EF se aplican **automáticamente al arrancar** `Flit.Api`; en Producción
> el contenedor **aborta si la migración falla**.

### 4.1 Instalar

```bash
apt install -y postgresql-16
systemctl enable --now postgresql
```

### 4.2 Usuario y base de datos — `flit` debe ser OWNER

El usuario `flit` **debe ser dueño** de la BD para poder `CREATE SCHEMA` (las migraciones
crean los esquemas `identity`, etc.). Si no es dueño, fallará con
`42501: permission denied for database`.

```bash
sudo -u postgres psql <<'EOF'
CREATE USER flit WITH ENCRYPTED PASSWORD '<PASSWORD_SEGURO>';
CREATE DATABASE flit_dev OWNER flit;
EOF
```

> **Importante:** si la BD ya existía con otro dueño (p. ej. del backend viejo),
> dale la propiedad a `flit`:
> ```bash
> sudo -u postgres psql -c "ALTER DATABASE flit_dev OWNER TO flit;"
> ```
> Si quieres empezar **limpio** (recomendado al migrar de un stack anterior), crea
> una BD nueva: `CREATE DATABASE flit_prod OWNER flit;` y apunta `CONNECTION_STRING_CORE`
> a `Database=flit_prod` (acuérdate de añadir su línea en `pg_hba.conf`, §4.4).

### 4.3 Escuchar en todas las interfaces

```bash
sed -i "s/#\?listen_addresses = 'localhost'/listen_addresses = '*'/" \
  /etc/postgresql/16/main/postgresql.conf
grep "^listen_addresses" /etc/postgresql/16/main/postgresql.conf   # => listen_addresses = '*'
```

### 4.4 Autorizar la red Docker en `pg_hba.conf`

Las redes de Docker Compose cambian de subred según el nombre del proyecto. En vez de
añadir cada subred una por una, autoriza **todo el rango privado de Docker** (`172.16.0.0/12`,
que cubre `172.16.x`–`172.31.x`). El método de auth es **md5** (el que ya usa el resto):

```bash
echo "host    flit_dev    flit    172.16.0.0/12    md5" \
  >> /etc/postgresql/16/main/pg_hba.conf
systemctl reload postgresql
```

> Si usas otra BD (p. ej. `flit_prod`), añade también su línea:
> `host flit_prod flit 172.16.0.0/12 md5`.

### 4.5 Verificar conexión desde un contenedor

```bash
docker run --rm --add-host=host.docker.internal:host-gateway postgres:16-alpine \
  psql "postgres://flit:<PASSWORD>@host.docker.internal:5432/flit_dev?sslmode=disable" -c "SELECT 1"
```

---

## 5. Configuración del firewall (UFW)

```bash
ufw allow OpenSSH
ufw allow 80/tcp   comment 'HTTP'
ufw allow 443/tcp  comment 'HTTPS'

# PostgreSQL — SOLO desde redes Docker internas (nunca 0.0.0.0).
# Rango amplio para que cualquier red de compose (subred variable) funcione.
ufw allow from 172.16.0.0/12 to any port 5432 proto tcp comment 'Postgres desde Docker'

ufw --force enable
ufw reload
ufw status verbose
```

> **Por qué el rango amplio:** el proyecto `flit-prod` obtiene una subred como
> `172.27.0.0/16`. Si solo permites `172.17`/`172.21` (subredes viejas), la conexión a
> Postgres dará **timeout** (paquete descartado por UFW). `172.16.0.0/12` las cubre todas.
> El 5432 nunca se expone al exterior.

---

## 6. Configuración de nginx y SSL

> Si vienes del despliegue anterior (puertos 8090/3030), basta con **cambiar el puerto**
> del `proxy_pass` en cada vhost existente (no reescribas el archivo: certbot ya le añadió
> los bloques SSL). Ejemplo con `sed`:
> ```bash
> sed -i 's#proxy_pass http://127.0.0.1:8090;#proxy_pass http://127.0.0.1:4001;#' \
>   /etc/nginx/sites-available/orca.flitsas.com
> sed -i 's#proxy_pass http://127.0.0.1:3030;#proxy_pass http://127.0.0.1:4002;#' \
>   /etc/nginx/sites-available/api.orca.flitsas.com
> nginx -t && systemctl reload nginx
> ```

### 6.1 Frontend (`orca.flitsas.com` → 4001)

```bash
cat > /etc/nginx/sites-available/orca.flitsas.com <<'EOF'
server {
    server_name orca.flitsas.com;
    location / {
        proxy_pass http://127.0.0.1:4001;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
    listen 80;
}
EOF
ln -sf /etc/nginx/sites-available/orca.flitsas.com /etc/nginx/sites-enabled/orca.flitsas.com
```

### 6.2 Gateway / API pública (`api.orca.flitsas.com` → 4002)

Incluye soporte WebSocket para SignalR (`/hubs`):

```bash
cat > /etc/nginx/sites-available/api.orca.flitsas.com <<'EOF'
server {
    server_name api.orca.flitsas.com;
    location / {
        proxy_pass http://127.0.0.1:4002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;            # WebSocket (/hubs)
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }
    listen 80;
}
EOF
ln -sf /etc/nginx/sites-available/api.orca.flitsas.com /etc/nginx/sites-enabled/api.orca.flitsas.com
```

### 6.3 (Opcional) Flit.Api directo (`core.orca.flitsas.com` → 4003)

Solo si necesitas pegarle al backend interno desde fuera (debug). La app **no** lo necesita.

```bash
cat > /etc/nginx/sites-available/core.orca.flitsas.com <<'EOF'
server {
    server_name core.orca.flitsas.com;
    location / {
        proxy_pass http://127.0.0.1:4003;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    listen 80;
}
EOF
ln -sf /etc/nginx/sites-available/core.orca.flitsas.com /etc/nginx/sites-enabled/core.orca.flitsas.com
```

### 6.4 Validar y emitir SSL

```bash
nginx -t && systemctl reload nginx
certbot --nginx -d orca.flitsas.com
certbot --nginx -d api.orca.flitsas.com
# certbot --nginx -d core.orca.flitsas.com   # solo si creaste el vhost 6.3
certbot renew --dry-run
```

---

## 7. Configuración de GitHub Secrets

**Settings → Secrets and variables → Actions → Secrets:**

| Secret | Descripción | Ejemplo |
|--------|-------------|---------|
| `HOSTINGER_SSH_HOST` | IP pública del VPS | `123.45.67.89` |
| `HOSTINGER_SSH_USER` | Usuario SSH | `root` |
| `HOSTINGER_SSH_KEY` | Clave privada ED25519 completa | `-----BEGIN OPENSSH...` |
| `HOSTINGER_DEPLOY_PATH` | Ruta de despliegue | `/opt/flit-boilerplate` |
| `GHCR_PAT` | PAT con scope `read:packages` (pull en el VPS) | `ghp_xxx...` |

**Variables** (Settings → … → Variables):

| Variable | Valor |
|----------|-------|
| `PUBLIC_DOMAIN` | `orca.flitsas.com` |

---

## 8. Variables de entorno del VPS

Archivo `/opt/flit-boilerplate/.env` (NO se despliega con CD; configúralo a mano).
Ver plantilla versionada en `.env.prod.example`.

```env
# Cadena Npgsql .NET (NO la URL postgres://). El código lee ConnectionStrings:Core.
CONNECTION_STRING_CORE=Host=host.docker.internal;Port=5432;Database=flit_dev;Username=flit;Password=<PASSWORD>;SSL Mode=Disable

# CORS del frontend (lo consume gateway y core-api)
CORS_ORIGIN=https://orca.flitsas.com

# Verifik (RUNT)
VERIFIK_API_TOKEN=<token>
VERIFIK_BASE_URL=https://api.verifik.co
VERIFIK_TIMEOUT_SECONDS=30

# Tags de imagen (opcional, default latest)
CORE_API_TAG=latest
FRONTEND_TAG=latest
```

> **Login deshabilitado:** no se necesitan `JWT_*` ni llaves en `./secrets/`.
> Para reactivar el login en el futuro, ver los comentarios en
> `Flit.Gateway/Program.cs` y `docker-compose.prod.yml`.

---

## 9. Primer despliegue

El CD se dispara con cada push a `develop`. Para desplegar:

```bash
# En tu máquina local
git push origin develop
```

El workflow `cd.yml`:
1. Build de `core-api` (contexto `./services/core-api`, publica Flit.Api + Flit.Gateway, JIT).
2. Build de `frontend` (contexto raíz, **pnpm**) con build-args **horneados en el bundle**:
   - `VITE_API_BASE_URL=https://api.orca.flitsas.com/api/v1`
   - `VITE_TRASPASOS_API_BASE_URL=https://api.orca.flitsas.com/api/v1/procedures`
   - `VITE_DEV_USER_ID=01900000-100b-7001-8001-000000000001`
3. Push a GHCR + `scp docker-compose.prod.yml` al VPS + `docker compose pull && up -d`.

> ⚠️ Como las `VITE_*` se hornean en build, **cambiarlas exige reconstruir** la imagen
> del frontend (no basta recrear el contenedor).

### Levantar / actualizar manualmente en el VPS

```bash
cd /opt/flit-boilerplate
echo "<GHCR_PAT>" | docker login ghcr.io -u <GITHUB_USER> --password-stdin
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d --remove-orphans
```

> Cambios solo en `docker-compose.prod.yml` (p. ej. `working_dir`, puertos, env) **no**
> requieren rebuild: edita el compose y `up -d --force-recreate <servicio>`.

---

## 10. Migraciones de base de datos

**Automáticas.** `Flit.Api` ejecuta `Database.MigrateAsync()` al arrancar. En Producción,
si una migración falla, el contenedor **aborta** (no arranca). No hay paso manual.

Salida esperada en el log del contenedor `core-api`:

```
[INF] Applying migration '20260522113228_InitialFlitV2'.
...
[INF] ✓ Migraciones EF Core aplicadas correctamente
[INF] Now listening on: http://[::]:8081
```

Ver el log: `docker logs flit-prod-core-api-1 --tail 50`

---

## 11. Verificación del sistema

De adentro hacia afuera (en el VPS):

```bash
# Contenedores (los 3 healthy; gateway y core-api comparten imagen core-api)
docker compose -f /opt/flit-boilerplate/docker-compose.prod.yml ps

# core-api directo (4003)
curl -i http://127.0.0.1:4003/api/v1/health        # 200 {"status":"ok",...}

# gateway (4002): propio + enrutando a core-api
curl -i http://127.0.0.1:4002/health               # 200 {"status":"alive"}
curl -i http://127.0.0.1:4002/api/v1/procedures?page=1&limit=10   # 200 (pasa por YARP)

# frontend (4001)
curl -I http://127.0.0.1:4001/                      # 200/3xx

# Público (a través de nginx + SSL)
curl -I https://orca.flitsas.com
curl -fsS https://api.orca.flitsas.com/api/v1/health
```

Logs en vivo:

```bash
docker logs -f flit-prod-core-api-1     # Flit.Api
docker logs -f flit-prod-gateway-1      # Flit.Gateway (YARP)
docker logs -f flit-prod-frontend-1     # nginx
```

---

## 12. Despliegues automáticos (CD)

Cada push a `develop` dispara `.github/workflows/cd.yml`:
1. Build + push de `core-api` y `frontend` a GHCR (tag `sha-xxxx` y `latest` en la rama por defecto).
2. `scp docker-compose.prod.yml` al VPS.
3. SSH → `docker compose pull` + `up -d` + health check a `127.0.0.1:4001` y `:4002/health`.

### Rollback

```bash
cd /opt/flit-boilerplate
docker images | grep flit-boilerplate          # ver tags/sha disponibles
# Fijar un sha en .env y recrear:
#   CORE_API_TAG=sha-xxxx   FRONTEND_TAG=sha-yyyy
docker compose -f docker-compose.prod.yml up -d
```

---

## 13. Troubleshooting

> Esta sección recoge los problemas reales que aparecieron al migrar al stack .NET nuevo.
> Diagnostica **por capas**: contenedor (4003/4002) → gateway → nginx → navegador.

### Build del frontend falla: `"/package-lock.json": not found`
El repo usa **pnpm**, no npm. El `frontend/Dockerfile` usa `corepack` + `pnpm install --frozen-lockfile`
con contexto = raíz. Si editas el Dockerfile, no vuelvas a `npm ci`.

### Build del core-api falla: `global.json / src: not found`
El contexto de build debe ser `./services/core-api` (no la raíz). Ver `cd.yml` job `build-core-api`.

### Build del gateway falla: `IL2026 / IL3050` (analizadores AOT)
`Flit.Gateway` hereda `IsAotCompatible=true`. El Dockerfile publica con
`/p:PublishAot=false /p:IsAotCompatible=false` (JIT). No quites esos flags.

### core-api en bucle de reinicio — `Timeout during connection attempt` a Postgres
La subred de la red Docker del compose no está permitida en **UFW**. Síntoma: *timeout*
(no "refused"). Verifica y corrige:
```bash
docker network inspect flit-prod_default -f '{{range .IPAM.Config}}{{.Subnet}}{{end}}'
ufw allow from 172.16.0.0/12 to any port 5432 proto tcp && ufw reload
```

### core-api: `no pg_hba.conf entry for host "172.x.x.x"`
Falta la entrada en `pg_hba.conf` (método **md5**):
```bash
echo "host    flit_dev    flit    172.16.0.0/12    md5" >> /etc/postgresql/16/main/pg_hba.conf
systemctl reload postgresql
```

### core-api: `42501: permission denied for database` (al `CREATE SCHEMA`)
El usuario `flit` no es dueño de la BD:
```bash
sudo -u postgres psql -c "ALTER DATABASE flit_dev OWNER TO flit;"
```
(o usa una BD nueva propiedad de `flit`).

### El WARN `CONNECTION_STRING_CORE is not set` al hacer `up`
Falta la variable en `/opt/flit-boilerplate/.env` (ver §8). Sin ella la app corre en
memoria (pierde datos) y rutas que dependen de repos solo-Postgres pueden dar 500.

### `404 Not Found` en `/api/*` desde el gateway (con header `X-Correlation-Id`)
El gateway arrancó **sin rutas YARP** porque no cargó su `appsettings.json`: el content
root debe ser su carpeta. En el compose cada servicio .NET debe tener `working_dir`:
```yaml
gateway:  { working_dir: /app/gateway }
core-api: { working_dir: /app/api }
```
Es cambio de compose (sin rebuild): edítalo y `up -d --force-recreate gateway core-api`.

### `500` en TODAS las rutas (incl. `/api/v1/health`), log: `Body was inferred...`
Un endpoint GET tenía un repo no registrado (sin Postgres) inferido como *body*, lo que
rompe el grafo de endpoints completo. Mitigado con `[FromServices]` y, sobre todo,
configurando `CONNECTION_STRING_CORE` (registra los repos vía EF).

### `400 Header X-User-Id requerido` al crear/editar trámites
Con login OFF, el frontend identifica al usuario con `X-User-Id`. El interceptor lo envía
en **todos** los ambientes (no solo dev) y el UUID viene de `VITE_DEV_USER_ID` (build-arg).
Si falla, reconstruye el frontend (el valor se hornea en el bundle).

### Detalle del trámite: errores Zod `expected ... received undefined`
El contrato del backend nuevo es más delgado que el `tramiteDetailSchema` del frontend.
`GET /api/v1/procedures/{id}` proyecta a la forma rica (ver `ProceduresEndpoints.cs`).
**Cuidado con los nulls:** el serializer usa `DefaultIgnoreCondition=WhenWritingNull`, que
**omite** propiedades null → el front las ve como `undefined` y un `z.string().nullable()`
falla. Para campos `nullable` no-opcionales, emite `""` en vez de `null`.

### nginx `502 Bad Gateway`
El contenedor destino no está arriba o cambió el puerto. Verifica
`docker compose ps` y que el `proxy_pass` del vhost apunte a `4001/4002/4003`.

### Imagen desactualizada en el VPS
`up -d` no baja imágenes nuevas si hay caché. Siempre:
```bash
docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d
```
Recuerda: los cambios de **build-args del frontend** o de **código backend** solo llegan
reconstruyendo la imagen (push a `develop` → CD), no recreando el contenedor.

### CORS en el navegador
`CORS_ORIGIN` debe coincidir exactamente con el dominio del front (`https://orca.flitsas.com`).
Tras cambiarlo: `docker compose -f docker-compose.prod.yml up -d --force-recreate gateway core-api`.

### SSL vencido
```bash
certbot renew && systemctl reload nginx
```
