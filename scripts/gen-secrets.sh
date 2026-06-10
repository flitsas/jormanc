#!/usr/bin/env bash
# gen-secrets.sh — Genera llaves JWT RS256 + passwords aleatorios para dev local.
# ADR-0002 §2 Decision 8 + §11 Habeas Data. Fase 8d del MIGRATION_PLAN.
#
# Uso:
#   ./scripts/gen-secrets.sh              # genera todo si no existe
#   ./scripts/gen-secrets.sh --force      # sobreescribe (PELIGROSO en prod)
#   ./scripts/gen-secrets.sh --check      # solo verifica existencia
#
# Salidas:
#   infra/secrets/jwt-private.pem        (RSA 4096, llave privada — NO commit)
#   infra/secrets/jwt-public.pem         (llave publica, puede ir cifrada con sops)
#   infra/secrets/.env.dev               (passwords aleatorios para docker-compose dev)
#
# Nota: jwt-private.pem y .env.dev quedan en .gitignore. jwt-public.pem es candidato
# a versionarse cifrado con sops + age (futura mejora).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SECRETS_DIR="$ROOT/infra/secrets"

FORCE=0
CHECK_ONLY=0

for arg in "$@"; do
  case "$arg" in
    --force) FORCE=1 ;;
    --check) CHECK_ONLY=1 ;;
    *) echo "Uso: $0 [--force | --check]"; exit 1 ;;
  esac
done

mkdir -p "$SECRETS_DIR"

PRIVATE_KEY="$SECRETS_DIR/jwt-private.pem"
PUBLIC_KEY="$SECRETS_DIR/jwt-public.pem"
DEV_ENV="$SECRETS_DIR/.env.dev"

echo ""
echo "FLIT Secrets Generator (Fase 8d)"
echo "================================"
echo ""

check_one() {
  local f="$1"
  if [[ -f "$f" ]]; then
    echo "  ✓ existe: ${f#$ROOT/}"
    return 0
  else
    echo "  ✗ falta:  ${f#$ROOT/}"
    return 1
  fi
}

if [[ $CHECK_ONLY -eq 1 ]]; then
  echo "Verificacion (modo --check):"
  missing=0
  check_one "$PRIVATE_KEY" || missing=$((missing + 1))
  check_one "$PUBLIC_KEY" || missing=$((missing + 1))
  check_one "$DEV_ENV" || missing=$((missing + 1))
  if [[ $missing -gt 0 ]]; then
    echo ""
    echo "Falta(n) $missing archivo(s). Ejecuta: ./scripts/gen-secrets.sh"
    exit 1
  fi
  echo "Todos los secretos estan en su lugar."
  exit 0
fi

generate_jwt_keys() {
  if [[ -f "$PRIVATE_KEY" && $FORCE -eq 0 ]]; then
    echo "  - jwt-private.pem ya existe (usa --force para sobreescribir)"
    return
  fi
  echo "  Generando RSA 4096 (puede tardar ~10s)..."
  openssl genrsa -out "$PRIVATE_KEY" 4096 2>/dev/null
  openssl rsa -in "$PRIVATE_KEY" -pubout -out "$PUBLIC_KEY" 2>/dev/null
  chmod 600 "$PRIVATE_KEY"
  chmod 644 "$PUBLIC_KEY"
  echo "  ✓ jwt-private.pem (chmod 600) generada"
  echo "  ✓ jwt-public.pem  (chmod 644) generada"
}

random_password() {
  # 32 caracteres alfanumericos + algunos especiales URL-safe
  openssl rand -base64 24 | tr -d '\n=/+'
}

generate_dev_env() {
  if [[ -f "$DEV_ENV" && $FORCE -eq 0 ]]; then
    echo "  - .env.dev ya existe (usa --force para sobreescribir)"
    return
  fi
  local pg_pwd rabbit_pwd minio_pwd grafana_pwd
  pg_pwd=$(random_password)
  rabbit_pwd=$(random_password)
  minio_pwd=$(random_password)
  grafana_pwd=$(random_password)

  cat > "$DEV_ENV" <<EOF
# infra/secrets/.env.dev — passwords aleatorios para docker-compose local.
# Generado por scripts/gen-secrets.sh el $(date -u +%Y-%m-%dT%H:%M:%SZ).
# NO commitear. Esta en .gitignore.

POSTGRES_PASSWORD=$pg_pwd
RABBITMQ_PASSWORD=$rabbit_pwd
MINIO_ROOT_PASSWORD=$minio_pwd
GRAFANA_ADMIN_PASSWORD=$grafana_pwd

# Para usar con compose de produccion adicionalmente:
PUBLIC_DOMAIN=localhost
CADDY_EMAIL=admin@example.local
EOF
  chmod 600 "$DEV_ENV"
  echo "  ✓ .env.dev (chmod 600) generado con passwords aleatorios de 24 bytes base64"
}

echo "1. Generando llaves JWT RS256..."
generate_jwt_keys
echo ""
echo "2. Generando .env.dev con passwords aleatorios..."
generate_dev_env
echo ""
echo "================================"
echo "✓ Secretos generados en infra/secrets/"
echo ""
echo "Validacion:"
openssl rsa -in "$PRIVATE_KEY" -noout -text 2>/dev/null | head -1 || echo "  ⚠ no se pudo validar private key"
openssl rsa -in "$PUBLIC_KEY" -pubin -noout -text 2>/dev/null | head -1 || echo "  ⚠ no se pudo validar public key"
echo ""
echo "Uso:"
echo "  - docker-compose: cargar con --env-file infra/secrets/.env.dev"
echo "  - .NET core-api:  montar jwt-private.pem y jwt-public.pem como secrets"
echo "  - Flit.Gateway / python-ml: SOLO jwt-public.pem (validacion local, post ADR-0014/0017)"
echo ""
echo "Seguridad:"
echo "  - jwt-private.pem NUNCA se distribuye fuera del host de core-api."
echo "  - .env.dev contiene passwords aleatorios para uso LOCAL UNICAMENTE."
echo "  - En produccion: usar sops + age con .sops.yaml (Fase 8 ampliada)."
echo ""
