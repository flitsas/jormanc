#!/usr/bin/env bash
# setup-env.sh — Pobla credenciales de tooling externo en .env raíz.
# Refactor .ai/ 2026-05-22.
#
# Variables manejadas:
#   AZURE_DEVOPS_PAT, AZURE_ORG, AZURE_PROJECT, AZURE_TEAM,
#   SONAR_TOKEN, SONAR_ORGANIZATION
#
# Uso:
#   ./scripts/setup-env.sh              # crea .env desde .env.example si no existe
#   ./scripts/setup-env.sh --interactive # prompts uno-a-uno
#   ./scripts/setup-env.sh --check       # CI: falla si faltan variables (no escribe)
#   ./scripts/setup-env.sh --print       # imprime valores enmascarados
#   ./scripts/setup-env.sh -h | --help   # ayuda
#
# Diferencias vs gen-secrets.sh:
#   - gen-secrets.sh  => JWT RS256 + passwords containers (en infra/secrets/.env.dev)
#   - setup-env.sh    => credenciales de tooling externo (en .env raíz)
#
# Compatible: macOS, Linux, Windows Git Bash (MINGW/MSYS), WSL. Bash 3.2+.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$ROOT/.env"
EXAMPLE="$ROOT/.env.example"
GITIGNORE="$ROOT/.gitignore"

# ---------------------------------------------------------------------------
# OS detection
# ---------------------------------------------------------------------------
case "$(uname -s)" in
  Linux*)               OS=linux ;;
  Darwin*)              OS=mac ;;
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  *)                    OS=unknown ;;
esac

# ---------------------------------------------------------------------------
# Colores (solo si tty)
# ---------------------------------------------------------------------------
if [ -t 1 ]; then
  C_OK=$'\033[32m'; C_WARN=$'\033[33m'; C_ERR=$'\033[31m'; C_DIM=$'\033[2m'; C_END=$'\033[0m'
else
  C_OK=""; C_WARN=""; C_ERR=""; C_DIM=""; C_END=""
fi

log()  { echo "[setup-env] $*"; }
ok()   { echo "${C_OK}[setup-env] ✓${C_END} $*"; }
warn() { echo "${C_WARN}[setup-env] ⚠${C_END} $*" >&2; }
err()  { echo "${C_ERR}[setup-env] ✗${C_END} $*" >&2; }

# ---------------------------------------------------------------------------
# Variables esperadas (orden = prompts del modo interactivo)
# ---------------------------------------------------------------------------
REQUIRED_VARS="AZURE_DEVOPS_PAT AZURE_ORG AZURE_PROJECT SONAR_TOKEN SONAR_ORGANIZATION"
OPTIONAL_VARS="AZURE_TEAM"

# Descripciones legibles (paralelo a REQUIRED_VARS — orden importa)
describe_var() {
  case "$1" in
    AZURE_DEVOPS_PAT)   echo "Personal Access Token Azure DevOps (scopes: Work Items r/w, Code r)" ;;
    AZURE_ORG)          echo "Organización Azure DevOps (ej: 'flit-company')" ;;
    AZURE_PROJECT)      echo "Proyecto Azure DevOps (recomendado: 'FLIT')" ;;
    AZURE_TEAM)         echo "Team default para queries (opcional)" ;;
    SONAR_TOKEN)        echo "Token SonarCloud (https://sonarcloud.io/account/security)" ;;
    SONAR_ORGANIZATION) echo "Slug organización SonarCloud" ;;
    *)                  echo "(sin descripción)" ;;
  esac
}

# ---------------------------------------------------------------------------
# Modo
# ---------------------------------------------------------------------------
MODE="create"  # create | interactive | check | print

usage() {
  sed -n '3,18p' "$0" | sed 's/^# //; s/^#//'
}

for arg in "$@"; do
  case "$arg" in
    --interactive|-i) MODE="interactive" ;;
    --check|-c)       MODE="check" ;;
    --print|-p)       MODE="print" ;;
    -h|--help)        usage; exit 0 ;;
    *)                err "Argumento desconocido: $arg"; usage; exit 1 ;;
  esac
done

# ---------------------------------------------------------------------------
# Pre-flight
# ---------------------------------------------------------------------------
if [ ! -f "$EXAMPLE" ]; then
  err ".env.example no existe en $EXAMPLE"
  err "Re-ejecuta tras checkout del repo limpio."
  exit 1
fi

# Verificar gitignore
ensure_gitignored() {
  if [ ! -f "$GITIGNORE" ]; then
    warn ".gitignore no existe — creando uno mínimo"
    echo ".env" > "$GITIGNORE"
    return
  fi
  if ! grep -qE '^\.env$|^/\.env$' "$GITIGNORE"; then
    warn ".env no está explícitamente en .gitignore."
    warn "Si vas a commitear este repo, añade manualmente '.env' a .gitignore."
  fi
}

# ---------------------------------------------------------------------------
# Helpers (read / write portable, sin sed -i incompatibilidades macOS/Linux)
# ---------------------------------------------------------------------------
read_env_var() {
  local var="$1"
  if [ -f "$ENV_FILE" ]; then
    awk -F= -v v="$var" '$1==v {sub(/^[^=]*=/,""); print; exit}' "$ENV_FILE"
  fi
}

write_env_var() {
  local var="$1" val="$2" tmp
  tmp=$(mktemp)
  if [ ! -f "$ENV_FILE" ]; then
    : > "$ENV_FILE"
  fi
  if grep -qE "^${var}=" "$ENV_FILE"; then
    awk -F= -v v="$var" -v val="$val" '
      BEGIN{updated=0}
      {
        if ($1 == v) { print v"="val; updated=1 }
        else { print }
      }
      END{ if(!updated) print v"="val }
    ' "$ENV_FILE" > "$tmp"
    mv "$tmp" "$ENV_FILE"
  else
    cat "$ENV_FILE" > "$tmp"
    echo "${var}=${val}" >> "$tmp"
    mv "$tmp" "$ENV_FILE"
  fi
}

mask_val() {
  local val="$1"
  local len=${#val}
  if [ "$len" -eq 0 ]; then echo "(vacío)"
  elif [ "$len" -le 4 ]; then echo "****"
  else echo "${val:0:4}…(${len} chars)"
  fi
}

# ---------------------------------------------------------------------------
# Modos
# ---------------------------------------------------------------------------
mode_check() {
  log "Modo --check: verificando $ENV_FILE"
  if [ ! -f "$ENV_FILE" ]; then
    err ".env no existe. Ejecuta: ./scripts/setup-env.sh --interactive"
    exit 1
  fi
  local missing=0
  for var in $REQUIRED_VARS; do
    val=$(read_env_var "$var")
    if [ -z "${val:-}" ]; then
      err "$var ausente o vacía"
      missing=$((missing + 1))
    else
      ok "$var presente"
    fi
  done
  for var in $OPTIONAL_VARS; do
    val=$(read_env_var "$var")
    if [ -z "${val:-}" ]; then
      log "(opcional) $var no definida — se usará default"
    else
      ok "(opcional) $var presente"
    fi
  done
  if [ "$missing" -gt 0 ]; then
    err "Faltan $missing variable(s) requerida(s)."
    err "Solución: ./scripts/setup-env.sh --interactive"
    exit 1
  fi
  ok "Todas las variables requeridas presentes."
}

mode_print() {
  if [ ! -f "$ENV_FILE" ]; then
    err ".env no existe."
    exit 1
  fi
  echo ""
  echo "Valores enmascarados de $ENV_FILE:"
  for var in $REQUIRED_VARS $OPTIONAL_VARS; do
    val=$(read_env_var "$var")
    printf "  %-22s = %s\n" "$var" "$(mask_val "${val:-}")"
  done
  echo ""
}

mode_create() {
  ensure_gitignored
  if [ -f "$ENV_FILE" ]; then
    log ".env ya existe en $ENV_FILE"
    log "Para editar: ./scripts/setup-env.sh --interactive"
    return 0
  fi
  cp "$EXAMPLE" "$ENV_FILE"
  chmod 600 "$ENV_FILE" 2>/dev/null || true
  ok ".env creado desde .env.example"
  warn "Variables requeridas con placeholders. Edita manualmente o ejecuta:"
  warn "  ./scripts/setup-env.sh --interactive"
}

mode_interactive() {
  ensure_gitignored
  if [ ! -f "$ENV_FILE" ]; then
    cp "$EXAMPLE" "$ENV_FILE"
    chmod 600 "$ENV_FILE" 2>/dev/null || true
    ok ".env creado desde .env.example"
  fi
  echo ""
  echo "Modo interactivo. Enter en blanco = conservar valor actual."
  echo "Para limpiar una variable: escribe la palabra exacta CLEAR"
  echo ""
  for var in $REQUIRED_VARS $OPTIONAL_VARS; do
    desc=$(describe_var "$var")
    cur=$(read_env_var "$var")
    display=$(mask_val "${cur:-}")
    echo "${C_DIM}  $desc${C_END}"
    printf "  %s [%s]: " "$var" "$display"
    # Read sin echo para PAT/Token (Bash 3.2 compat)
    case "$var" in
      *PAT*|*TOKEN*)
        if [ -t 0 ]; then
          stty -echo 2>/dev/null || true
          read -r new || true
          stty echo 2>/dev/null || true
          echo ""
        else
          read -r new || true
        fi
        ;;
      *)
        read -r new || true
        ;;
    esac
    if [ -z "${new:-}" ]; then
      log "$var sin cambios"
    elif [ "$new" = "CLEAR" ]; then
      write_env_var "$var" ""
      ok "$var limpiada"
    else
      write_env_var "$var" "$new"
      ok "$var actualizada"
    fi
    echo ""
  done
  ok "Setup interactivo completado."
  log "Verifica con: ./scripts/setup-env.sh --check"
}

# ---------------------------------------------------------------------------
# Header
# ---------------------------------------------------------------------------
echo ""
echo "FLIT setup-env (.env raíz — tooling externo)"
echo "============================================="
echo "OS: $OS · modo: $MODE"

case "$MODE" in
  create)      mode_create ;;
  check)       mode_check ;;
  print)       mode_print ;;
  interactive) mode_interactive ;;
esac

echo ""
